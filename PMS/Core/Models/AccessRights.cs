using MongoDB.Bson.Serialization.Attributes;

namespace PMS.Core.Models;

public class AccessRights
{
    [BsonElement("view_data")]
    public bool ViewData { get; set; }

    [BsonElement("edit_data")]
    public bool EditData { get; set; }

    [BsonElement("delete_data")]
    public bool DeleteData { get; set; }

    [BsonElement("run_aggregations")]
    public bool RunAggregations { get; set; }

    [BsonElement("save_results")]
    public bool SaveResults { get; set; }

    [BsonElement("manage_users")]
    public bool ManageUsers { get; set; }
}