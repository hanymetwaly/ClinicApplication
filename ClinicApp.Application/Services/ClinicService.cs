using ClinicApp.Application.DTOs;
using ClinicApp.Application.Exceptions;
using ClinicApp.Application.Interfaces;
using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using ClinicApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicApp.Application.Services;

public class ClinicService : IClinicService
{
    private const int MaximumPageSize = 100;
    private readonly IClinicDbContext _context;
    private readonly ILogger<ClinicService> _logger;
    private readonly IPatientRepository _patientRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IFileStorageService _fileStorage;

    public ClinicService(
        IClinicDbContext context,
        ILogger<ClinicService> logger,
        IPatientRepository patientRepository,
        IAppointmentRepository appointmentRepository,
        IInvoiceRepository invoiceRepository,
        IPasswordHasher passwordHasher,
        IFileStorageService fileStorage)
    {
        _context = context;
        _logger = logger;
        _patientRepository = patientRepository;
        _appointmentRepository = appointmentRepository;
        _invoiceRepository = invoiceRepository;
        _passwordHasher = passwordHasher;
        _fileStorage = fileStorage;
    }

    public async Task SeedInitialDataAsync()
    {
        if (!await _context.Roles.IgnoreQueryFilters().AnyAsync())
        {
            await _context.AddRangeAsync(
            [
                new Role { Id = 1, Name = RoleNames.Admin },
                new Role { Id = 2, Name = RoleNames.Doctor },
                new Role { Id = 3, Name = RoleNames.Receptionist }
            ]);
            await _context.SaveChangesAsync();
        }

        if (!await _context.Users.IgnoreQueryFilters().AnyAsync())
        {
            await _context.AddRangeAsync(
            [
                new User { Id = Guid.NewGuid(), Username = "admin", PasswordHash = _passwordHasher.HashPassword("admin"), RoleId = 1 },
                new User { Id = Guid.NewGuid(), Username = "doctor", PasswordHash = _passwordHasher.HashPassword("doctor"), RoleId = 2 },
                new User { Id = Guid.NewGuid(), Username = "receptionist", PasswordHash = _passwordHasher.HashPassword("receptionist"), RoleId = 3 }
            ]);
        }

        if (!await _context.Doctors.IgnoreQueryFilters().AnyAsync())
        {
            await _context.AddAsync(new Doctor
            {
                Id = Guid.NewGuid(),
                FullName = "Dr. Sarah Ahmed",
                Specialty = "Cardiology",
                Email = "sarah@example.com"
            });
        }

        if (!await _context.Patients.IgnoreQueryFilters().AnyAsync())
        {
            await _context.AddAsync(new Patient
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                PhoneNumber = "0501234567",
                Email = "john@example.com",
                DateOfBirth = new DateOnly(1988, 4, 10),
                MedicalHistory = "Asthma",
                InsuranceInfo = "MedNet"
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        _logger.LogInformation("Login attempt for {Username}", username);
        var normalizedUsername = username.Trim();
        var user = await _context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Username == normalizedUsername);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        return _passwordHasher.VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public async Task<PagedResult<PatientDto>> GetPatientsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "fullName",
        bool descending = false)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var result = await _patientRepository.GetPagedAsync(search?.Trim(), page, pageSize, sortBy, descending);
        return MapPage(result, MapPatient);
    }

    public async Task<PatientDto> GetPatientAsync(Guid id)
    {
        var patient = await _patientRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Patient was not found.");
        return MapPatient(patient);
    }

    public async Task<PatientDto> CreatePatientAsync(CreatePatientRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _patientRepository.ExistsAsync(patient => patient.Email == normalizedEmail && !patient.IsDeleted))
        {
            throw new ConflictException("A patient with this email already exists.");
        }

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = normalizedEmail,
            DateOfBirth = request.DateOfBirth,
            MedicalHistory = request.MedicalHistory.Trim(),
            InsuranceInfo = request.InsuranceInfo.Trim()
        };

        await _patientRepository.AddAsync(patient);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created patient {PatientId}", patient.Id);
        return MapPatient(patient);
    }

    public async Task<PatientDto> UpdatePatientAsync(Guid id, UpdatePatientRequest request)
    {
        var patient = await _patientRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Patient was not found.");
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _patientRepository.ExistsAsync(candidate =>
            candidate.Id != id &&
            candidate.Email == normalizedEmail &&
            !candidate.IsDeleted))
        {
            throw new ConflictException("A patient with this email already exists.");
        }

        patient.FullName = request.FullName.Trim();
        patient.PhoneNumber = request.PhoneNumber.Trim();
        patient.Email = normalizedEmail;
        patient.DateOfBirth = request.DateOfBirth;
        patient.MedicalHistory = request.MedicalHistory.Trim();
        patient.InsuranceInfo = request.InsuranceInfo.Trim();

        await _patientRepository.UpdateAsync(patient);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated patient {PatientId}", patient.Id);
        return MapPatient(patient);
    }

    public async Task DeletePatientAsync(Guid id)
    {
        var patient = await _patientRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Patient was not found.");
        await _patientRepository.DeleteAsync(patient);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Soft deleted patient {PatientId}", id);
    }

    public async Task<IReadOnlyList<DoctorDto>> GetDoctorsAsync()
    {
        return await _context.Doctors
            .Where(doctor => doctor.IsActive)
            .OrderBy(doctor => doctor.FullName)
            .Select(doctor => new DoctorDto
            {
                Id = doctor.Id,
                FullName = doctor.FullName,
                Specialty = doctor.Specialty,
                Email = doctor.Email
            })
            .ToListAsync();
    }

    public async Task<PagedResult<DoctorDto>> GetDoctorsAsync(int page = 1, int pageSize = 10, string? sortBy = "fullName", bool descending = false)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _context.Doctors
            .Where(doctor => doctor.IsActive)
            .Select(doctor => new DoctorDto
            {
                Id = doctor.Id,
                FullName = doctor.FullName,
                Specialty = doctor.Specialty,
                Email = doctor.Email
            })
            .AsQueryable();

        query = (sortBy?.ToLower()) switch
        {
            "fullname" => descending ? query.OrderByDescending(d => d.FullName) : query.OrderBy(d => d.FullName),
            "specialty" => descending ? query.OrderByDescending(d => d.Specialty) : query.OrderBy(d => d.Specialty),
            _ => descending ? query.OrderByDescending(d => d.FullName) : query.OrderBy(d => d.FullName)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<DoctorDto>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<DoctorDto>> LookupDoctorsAsync(string? query = null, int limit = 20)
    {
        var q = _context.Doctors
            .Where(doctor => doctor.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(d => d.FullName.Contains(term) || d.Specialty.Contains(term));
        }

        return await q
            .OrderBy(d => d.FullName)
            .Select(d => new DoctorDto { Id = d.Id, FullName = d.FullName, Specialty = d.Specialty, Email = d.Email })
            .Take(limit)
            .ToListAsync();
    }

    public async Task<PagedResult<AppointmentDto>> GetAppointmentsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? doctorId = null,
        AppointmentStatus? status = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "startTime",
        bool descending = false)
    {
        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
        {
            throw new RequestValidationException("End date must not be earlier than start date.");
        }

        (page, pageSize) = NormalizePagination(page, pageSize);
        var result = await _appointmentRepository.GetPagedAsync(
            startDate,
            endDate,
            doctorId,
            status,
            page,
            pageSize,
            sortBy,
            descending);
        return MapPage(result, MapAppointment);
    }

    public async Task<AppointmentDto> GetAppointmentAsync(Guid id)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Appointment was not found.");
        return MapAppointment(appointment);
    }

    public async Task<AppointmentDto> BookAppointmentAsync(CreateAppointmentRequest request)
    {
        Appointment? appointment = null;
        await _context.ExecuteInTransactionAsync(async cancellationToken =>
        {
            await ValidateAppointmentReferencesAsync(request.PatientId, request.DoctorId);
            if (!await _appointmentRepository.IsSlotAvailableAsync(
                request.DoctorId,
                request.StartTime,
                request.EndTime))
            {
                throw new ConflictException("The selected doctor already has an appointment in this time range.");
            }

            appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Notes = request.Notes.Trim(),
                Status = AppointmentStatus.Scheduled
            };
            await _appointmentRepository.AddAsync(appointment);
            await _context.SaveChangesAsync(cancellationToken);
        });

        _logger.LogInformation("Booked appointment {AppointmentId}", appointment!.Id);
        return await GetAppointmentAsync(appointment.Id);
    }

    public async Task<AppointmentDto> CancelAppointmentAsync(Guid id)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Appointment was not found.");
        appointment.Status = AppointmentStatus.Cancelled;
        await _appointmentRepository.UpdateAsync(appointment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Cancelled appointment {AppointmentId}", id);
        return MapAppointment(appointment);
    }

    public async Task<AppointmentDto> RescheduleAppointmentAsync(Guid id, RescheduleAppointmentRequest request)
    {
        await _context.ExecuteInTransactionAsync(async cancellationToken =>
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Appointment was not found.");
            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                throw new ConflictException("A cancelled appointment cannot be rescheduled.");
            }

            if (!await _appointmentRepository.IsSlotAvailableAsync(
                appointment.DoctorId,
                request.StartTime,
                request.EndTime,
                id))
            {
                throw new ConflictException("The selected doctor already has an appointment in this time range.");
            }

            appointment.StartTime = request.StartTime;
            appointment.EndTime = request.EndTime;
            await _appointmentRepository.UpdateAsync(appointment);
            await _context.SaveChangesAsync(cancellationToken);
        });

        _logger.LogInformation("Rescheduled appointment {AppointmentId}", id);
        return await GetAppointmentAsync(id);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            throw new RequestValidationException("An invoice must contain at least one item.");
        }

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new RequestValidationException("Item quantity must be greater than zero.");
            }

            if (item.UnitPrice <= 0)
            {
                throw new RequestValidationException("Item unit price must be greater than zero.");
            }
        }

        Invoice? invoice = null;
        await _context.ExecuteInTransactionAsync(async cancellationToken =>
        {
            if (!await _context.Patients.AnyAsync(patient => patient.Id == request.PatientId, cancellationToken))
            {
                throw new NotFoundException("Patient was not found.");
            }

            invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                VatRate = request.VatRate,
                DiscountRate = request.DiscountRate,
                Items = request.Items.Select(item => new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = item.Description.Trim(),
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = decimal.Round(item.Quantity * item.UnitPrice, 2)
                }).ToList()
            };
            invoice.TotalAmount = invoice.Items.Sum(item => item.TotalPrice);
            invoice.VatAmount = decimal.Round(invoice.TotalAmount * invoice.VatRate / 100m, 2);
            invoice.DiscountAmount = decimal.Round(invoice.TotalAmount * invoice.DiscountRate / 100m, 2);
            invoice.NetAmount = invoice.TotalAmount + invoice.VatAmount - invoice.DiscountAmount;
            invoice.Status = InvoiceStatus.Draft;

            await _invoiceRepository.AddAsync(invoice);
            await _context.SaveChangesAsync(cancellationToken);
        });

        _logger.LogInformation("Created invoice {InvoiceId}", invoice!.Id);
        return await GetInvoiceAsync(invoice.Id);
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(
        Guid? patientId = null,
        InvoiceStatus? status = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "invoiceDate",
        bool descending = true)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var result = await _invoiceRepository.GetPagedAsync(
            patientId,
            status,
            page,
            pageSize,
            sortBy,
            descending);
        return MapPage(result, MapInvoice);
    }

    public async Task<InvoiceDto> GetInvoiceAsync(Guid id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Invoice was not found.");
        return MapInvoice(invoice);
    }

    public async Task<PaymentDto> PayInvoiceAsync(Guid invoiceId, decimal amount)
    {
        Payment? payment = null;
        await _context.ExecuteInTransactionAsync(async cancellationToken =>
        {
            var invoice = await _context.Invoices
                .Include(invoice => invoice.Payments)
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId)
                ?? throw new NotFoundException("Invoice was not found.");
            var paidAmount = invoice.Payments.Sum(existingPayment => existingPayment.Amount);
            var outstandingAmount = invoice.NetAmount - paidAmount;

            if (invoice.Status == InvoiceStatus.Paid)
            {
                throw new ConflictException("The invoice is already paid.");
            }

            if (outstandingAmount <= 0)
            {
                throw new ConflictException("No outstanding amount to pay.");
            }

            if (amount > outstandingAmount)
            {
                throw new RequestValidationException(
                    $"Payment cannot exceed the outstanding amount of {outstandingAmount:0.00}.");
            }

            payment = new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Amount = amount
            };
            await _context.AddAsync(payment);

            var newPaidAmount = paidAmount + amount;
            invoice.Status = newPaidAmount >= invoice.NetAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.Partial;
            await _context.SaveChangesAsync(cancellationToken);
        });

        _logger.LogInformation(
            "Recorded payment {PaymentId} for invoice {InvoiceId}",
            payment!.Id,
            invoiceId);
        return new PaymentDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt
        };
    }

    public async Task<DashboardSummary> GetDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        return new DashboardSummary
        {
            TodayAppointments = await _context.Appointments.CountAsync(appointment =>
                appointment.StartTime >= today &&
                appointment.StartTime < tomorrow &&
                appointment.Status != AppointmentStatus.Cancelled),
            TotalPatients = await _context.Patients.CountAsync(),
            Revenue = await _context.Payments.SumAsync(payment => (decimal?)payment.Amount) ?? 0m,
            UnpaidInvoices = await _context.Invoices.CountAsync(invoice =>
                invoice.Status != InvoiceStatus.Paid)
        };
    }

    public async Task<DashboardChartData> GetDashboardChartDataAsync()
    {
        var today = DateTime.UtcNow.Date;
        var appointmentLabels = new List<string>();
        var appointmentData = new List<decimal>();
        for (var i = -6; i <= 7; i++)
        {
            var day = today.AddDays(i);
            var nextDay = day.AddDays(1);
            var count = await _context.Appointments.CountAsync(appointment =>
                appointment.StartTime >= day &&
                appointment.StartTime < nextDay &&
                appointment.Status != AppointmentStatus.Cancelled);
            appointmentLabels.Add(day.ToString("MMM d"));
            appointmentData.Add(count);
        }

        var revenueLabels = new List<string>();
        var revenueData = new List<decimal>();
        for (var i = 5; i >= 0; i--)
        {
            var monthStart = today.AddMonths(-i).AddDays(1 - today.Day);
            var nextMonth = monthStart.AddMonths(1);
            var amount = await _context.Payments
                .Where(payment => payment.PaidAt >= monthStart && payment.PaidAt < nextMonth)
                .SumAsync(payment => (decimal?)payment.Amount) ?? 0m;
            revenueLabels.Add(monthStart.ToString("MMM"));
            revenueData.Add(amount);
        }

        return new DashboardChartData
        {
            Appointments = new ChartSeries { Labels = appointmentLabels, Data = appointmentData },
            Revenue = new ChartSeries { Labels = revenueLabels, Data = revenueData }
        };
    }

    private async Task ValidateAppointmentReferencesAsync(Guid patientId, Guid doctorId)
    {
        if (!await _context.Patients.AnyAsync(patient => patient.Id == patientId))
        {
            throw new NotFoundException("Patient was not found.");
        }

        if (!await _context.Doctors.AnyAsync(doctor => doctor.Id == doctorId && doctor.IsActive))
        {
            throw new NotFoundException("Doctor was not found or is inactive.");
        }
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new RequestValidationException("Page must be at least 1.");
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw new RequestValidationException(
                $"Page size must be between 1 and {MaximumPageSize}.");
        }

        return (page, pageSize);
    }

    private static PagedResult<TDestination> MapPage<TSource, TDestination>(
        PagedResult<TSource> source,
        Func<TSource, TDestination> map)
    {
        return new PagedResult<TDestination>(
            source.Items.Select(map).ToList(),
            source.TotalCount,
            source.Page,
            source.PageSize);
    }

    public async Task<PatientDocumentDto> UploadPatientDocumentAsync(Guid patientId, string fileName, string contentType, long size, Stream content)
    {
        if (!await _patientRepository.ExistsAsync(p => p.Id == patientId && !p.IsDeleted))
        {
            throw new NotFoundException("Patient was not found.");
        }

        if (size <= 0)
        {
            throw new RequestValidationException("File size must be greater than zero.");
        }

        var storedFileName = await _fileStorage.SaveAsync(content, fileName);
        var document = new PatientDocument
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            FileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            Size = size
        };
        await _context.AddAsync(document);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Uploaded document {DocumentId} for patient {PatientId}", document.Id, patientId);
        return MapPatientDocument(document);
    }

    public async Task<IReadOnlyList<PatientDocumentDto>> GetPatientDocumentsAsync(Guid patientId)
    {
        if (!await _patientRepository.ExistsAsync(p => p.Id == patientId && !p.IsDeleted))
        {
            throw new NotFoundException("Patient was not found.");
        }

        return await _context.PatientDocuments
            .Where(d => d.PatientId == patientId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => MapPatientDocument(d))
            .ToListAsync();
    }

    private static PatientDto MapPatient(Patient patient) => new()
    {
        Id = patient.Id,
        FullName = patient.FullName,
        PhoneNumber = patient.PhoneNumber,
        Email = patient.Email,
        DateOfBirth = patient.DateOfBirth,
        MedicalHistory = patient.MedicalHistory,
        InsuranceInfo = patient.InsuranceInfo,
        DocumentCount = patient.Documents?.Count ?? 0,
        CreatedAt = patient.CreatedAt,
        UpdatedAt = patient.UpdatedAt
    };

    private static PatientDocumentDto MapPatientDocument(PatientDocument document) => new()
    {
        Id = document.Id,
        PatientId = document.PatientId,
        FileName = document.FileName,
        ContentType = document.ContentType,
        Size = document.Size,
        UploadedAt = document.UploadedAt
    };

    private static AppointmentDto MapAppointment(Appointment appointment) => new()
    {
        Id = appointment.Id,
        PatientId = appointment.PatientId,
        PatientName = appointment.Patient?.FullName ?? string.Empty,
        DoctorId = appointment.DoctorId,
        DoctorName = appointment.Doctor?.FullName ?? string.Empty,
        StartTime = DateTime.SpecifyKind(appointment.StartTime, DateTimeKind.Utc),
        EndTime = DateTime.SpecifyKind(appointment.EndTime, DateTimeKind.Utc),
        Status = appointment.Status,
        Notes = appointment.Notes
    };

    private static InvoiceDto MapInvoice(Invoice invoice)
    {
        var paidAmount = invoice.Payments.Sum(payment => payment.Amount);
        return new InvoiceDto
        {
            Id = invoice.Id,
            PatientId = invoice.PatientId,
            PatientName = invoice.Patient?.FullName ?? string.Empty,
            InvoiceDate = invoice.InvoiceDate,
            TotalAmount = invoice.TotalAmount,
            VatRate = invoice.VatRate,
            VatAmount = invoice.VatAmount,
            DiscountRate = invoice.DiscountRate,
            DiscountAmount = invoice.DiscountAmount,
            NetAmount = invoice.NetAmount,
            PaidAmount = paidAmount,
            OutstandingAmount = Math.Max(0m, invoice.NetAmount - paidAmount),
            Status = invoice.Status,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
    }
}
