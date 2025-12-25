using System.Threading.Tasks;
using PMS.Core.Models.DTO;

namespace PMS.Core.Services.Interfaces
{
    public interface IPdfService
    {
        Task<string> GenerateCertificatePdfAsync(CertificateDetail certificate, string outputPath);
    }
}
