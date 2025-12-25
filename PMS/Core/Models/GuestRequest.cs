using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

[BsonIgnoreExtraElements]
public partial class GuestRequest : BaseEntity
{

    [BsonElement("guest_user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [Required(ErrorMessage = "Guest User ID є обов'язковим")]
    public string GuestUserId { get; set; } = string.Empty;

    [BsonElement("request_date")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Required(ErrorMessage = "Дата заявки є обов'язковою")]
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    [BsonElement("status")]
    [Required(ErrorMessage = "Статус є обов'язковим")]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    [BsonElement("message")]
    public string? Message { get; set; }

    [BsonElement("login")]
    [Required(ErrorMessage = "Логін є обов'язковим")]
    [MinLength(3, ErrorMessage = "Логін має містити мінімум 3 символи")]
    public string Login { get; set; } = string.Empty;

    [BsonElement("email")]
    [Required(ErrorMessage = "Email є обов'язковим")]
    [EmailAddress(ErrorMessage = "Невірний формат email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    [Required(ErrorMessage = "Хеш паролю є обов'язковим")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("mobile_phone")]
    [Phone(ErrorMessage = "Невірний формат номера телефону")]
    public string? MobilePhone { get; set; }

    [BsonElement("full_name")]
    [Required(ErrorMessage = "ПІБ є обов'язковим")]
    [MinLength(1, ErrorMessage = "ПІБ не може бути порожнім")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("organization")]
    public string? Organization { get; set; }

    [BsonElement("position")]
    public string? Position { get; set; }

    [BsonElement("request_code")]
    [Required(ErrorMessage = "Код заявки є обов'язковим")]
    [RegularExpression(@"^[A-Z0-9]{8}$", ErrorMessage = "Код заявки має містити 8 символів (A-Z, 0-9)")]
    public string RequestCode { get; set; } = string.Empty;

    [BsonElement("admin_response")]
    public string? AdminResponse { get; set; }

    [BsonElement("processed_date")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ProcessedDate { get; set; }

    [BsonElement("processed_by")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ProcessedBy { get; set; }
}