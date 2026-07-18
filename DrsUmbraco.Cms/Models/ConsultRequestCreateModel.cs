using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DrsUmbraco.Cms.Models;

public sealed class ConsultRequestCreateModel
{
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره تماس الزامی است.")]
    [StringLength(30)]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "نوع درخواست الزامی است.")]
    [StringLength(60)]
    public string RequestType { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Message { get; set; }

    [StringLength(80)]
    public string? FormName { get; set; }
    public IFormFile? ResumeFile { get; set; }
}