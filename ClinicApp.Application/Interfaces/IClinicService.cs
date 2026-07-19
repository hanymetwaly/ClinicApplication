using ClinicApp.Application.DTOs;
using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;

namespace ClinicApp.Application.Interfaces;

public interface IClinicService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<PagedResult<PatientDto>> GetPatientsAsync(string? search = null, int page = 1, int pageSize = 10, string? sortBy = "fullName", bool descending = false);
    Task<PatientDto> GetPatientAsync(Guid id);
    Task<PatientDto> CreatePatientAsync(CreatePatientRequest request);
    Task<PatientDto> UpdatePatientAsync(Guid id, UpdatePatientRequest request);
    Task DeletePatientAsync(Guid id);
    Task<IReadOnlyList<DoctorDto>> GetDoctorsAsync();
    Task<PagedResult<DoctorDto>> GetDoctorsAsync(int page = 1, int pageSize = 10, string? sortBy = "fullName", bool descending = false);
    Task<IReadOnlyList<DoctorDto>> LookupDoctorsAsync(string? query = null, int limit = 20);
    Task<PagedResult<AppointmentDto>> GetAppointmentsAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? doctorId = null, AppointmentStatus? status = null, int page = 1, int pageSize = 10, string? sortBy = "startTime", bool descending = false);
    Task<AppointmentDto> GetAppointmentAsync(Guid id);
    Task<AppointmentDto> BookAppointmentAsync(CreateAppointmentRequest request);
    Task<AppointmentDto> CancelAppointmentAsync(Guid id);
    Task<AppointmentDto> RescheduleAppointmentAsync(Guid id, RescheduleAppointmentRequest request);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<PagedResult<InvoiceDto>> GetInvoicesAsync(Guid? patientId = null, InvoiceStatus? status = null, int page = 1, int pageSize = 10, string? sortBy = "invoiceDate", bool descending = true);
    Task<InvoiceDto> GetInvoiceAsync(Guid id);
    Task<PaymentDto> PayInvoiceAsync(Guid invoiceId, decimal amount);
    Task<DashboardSummary> GetDashboardAsync();
    Task<DashboardChartData> GetDashboardChartDataAsync();
    Task<PatientDocumentDto> UploadPatientDocumentAsync(Guid patientId, string fileName, string contentType, long size, Stream content);
    Task<IReadOnlyList<PatientDocumentDto>> GetPatientDocumentsAsync(Guid patientId);
    Task SeedInitialDataAsync();
}
