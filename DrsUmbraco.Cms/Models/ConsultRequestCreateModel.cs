using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DrsUmbraco.Cms.Models;

public sealed class ConsultRequestCreateModel
{
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "نام و نام خانوادگی باید بین 2 تا 100 کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره تماس الزامی است.")]
    [RegularExpression(@"^09[0-9]{9}$", ErrorMessage = "شماره موبایل باید با 09 شروع شود و دقیقاً 11 رقم باشد.")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "نوع درخواست الزامی است.")]
    [StringLength(60)]
    public string RequestType { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Message { get; set; }

    [Required(ErrorMessage = "نوع فرم الزامی است.")]
    [StringLength(80)]
    public string FormName { get; set; } = string.Empty;
    public IFormFile? ResumeFile { get; set; }
}