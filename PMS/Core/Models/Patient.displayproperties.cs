using System;
using MongoDB.Bson.Serialization.Attributes;
using PMS.Core.Enums.General;

namespace PMS.Core.Models;

public partial class Patient
{
    [BsonIgnore]
    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Year;
            if (BirthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    [BsonIgnore]
    public string AgeDisplay => $"{Age} р.";


    [BsonIgnore]
    public string HealthStatusDisplay => HealthStatus switch
    {
        HealthStatus.Healthy => "Відносно здоровий",
        HealthStatus.Chronic => "Хронічн(е/і) захворювання",
        HealthStatus.Acute => "Гостр(е/і) захворювання",
        HealthStatus.Recovery => "Одужання",
        HealthStatus.Observation => "Під наглядом",
        _ => "Не вказано"
    };



    [BsonIgnore]
    public string StatusDisplay => IsActive ? "Активний" : "Архів";

    [BsonIgnore]
    public string AddressDisplay => Address?.ToShortString() ?? "Не вказано";





    [BsonIgnore]
    public string AssignedDoctorName { get; set; } = "Не призначено";



}