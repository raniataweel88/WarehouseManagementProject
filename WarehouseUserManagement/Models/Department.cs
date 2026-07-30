using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WarehouseUserManagement.Controllers;
using WarehouseUserManagement.Data;

namespace WarehouseUserManagement.Models
{
    /// <summary>
    /// جدول الأقسام - Departments table (المستفيدة من سندات الصرف).
    /// DepartmentName has a unique index (see AppDbContext) -> O(log n) lookup بدل O(n) Scan.
    /// One Department -> Many Issues (سيتم ربطها لاحقاً عند بناء موديول الصرف).
    /// </summary>
    public class Department
    {
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [MinLength(2, ErrorMessage = "اسم القسم يجب أن لا يقل عن 2 حرف")]
        [MaxLength(100)]
        [Display(Name = "اسم القسم")]
        public string DepartmentName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        // Concurrency token -> يمنع Lost Update عند تعديل نفس القسم من جلستين بنفس الوقت
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}