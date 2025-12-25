using MongoDB.Bson.Serialization;
using PMS.Core.Enums.General;

namespace PMS.Core.Database;

public static class MongoDbSettings
{
    public static void ConfigureEnums()
    {
        BsonSerializer.RegisterSerializer(typeof(BloodType), new EnumMemberSerializer<BloodType>());
        BsonSerializer.RegisterSerializer(typeof(UserRole), new EnumMemberSerializer<UserRole>());
        BsonSerializer.RegisterSerializer(typeof(AppointmentType), new EnumMemberSerializer<AppointmentType>());
        BsonSerializer.RegisterSerializer(typeof(AppointmentStatus), new EnumMemberSerializer<AppointmentStatus>());
        BsonSerializer.RegisterSerializer(typeof(RoomType), new EnumMemberSerializer<RoomType>());
        BsonSerializer.RegisterSerializer(typeof(EntityType), new EnumMemberSerializer<EntityType>());
        BsonSerializer.RegisterSerializer(typeof(Shift), new EnumMemberSerializer<Shift>());
        BsonSerializer.RegisterSerializer(typeof(CertificateType), new EnumMemberSerializer<CertificateType>());
        BsonSerializer.RegisterSerializer(typeof(Urgency), new EnumMemberSerializer<Urgency>());
        BsonSerializer.RegisterSerializer(typeof(HomeVisitStatus), new EnumMemberSerializer<HomeVisitStatus>());
        BsonSerializer.RegisterSerializer(typeof(ProcedureType), new EnumMemberSerializer<ProcedureType>());
        BsonSerializer.RegisterSerializer(typeof(ProcedureStatus), new EnumMemberSerializer<ProcedureStatus>());
        BsonSerializer.RegisterSerializer(typeof(RequestStatus), new EnumMemberSerializer<RequestStatus>());
        BsonSerializer.RegisterSerializer(typeof(DatabaseAccessLevel), new EnumMemberSerializer<DatabaseAccessLevel>());
        BsonSerializer.RegisterSerializer(typeof(DoctorCategory), new EnumMemberSerializer<DoctorCategory>());
        BsonSerializer.RegisterSerializer(typeof(Gender), new EnumMemberSerializer<Gender>());
        BsonSerializer.RegisterSerializer(typeof(HealthStatus), new EnumMemberSerializer<HealthStatus>());
    }
}