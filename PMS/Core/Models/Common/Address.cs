using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace PMS.Core.Models.Common;

public class Address
{
    [BsonElement("street")]
    public string Street { get; set; } = string.Empty;

    [BsonElement("building")]
    public string Building { get; set; } = string.Empty;

    [BsonElement("apartment")]
    public string? Apartment { get; set; }

    [BsonElement("entrance")]
    public string? Entrance { get; set; }

    [BsonElement("floor")]
    public int? Floor { get; set; }

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("district")]
    public string District { get; set; } = string.Empty;

    [BsonElement("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    public override string ToString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(City))
            parts.Add($"м. {City}");

        if (!string.IsNullOrWhiteSpace(Street))
            parts.Add($"вул. {Street}");

        if (!string.IsNullOrWhiteSpace(Building))
            parts.Add($"буд. {Building}");

        if (!string.IsNullOrWhiteSpace(Apartment))
            parts.Add($"кв. {Apartment}");

        if (!string.IsNullOrWhiteSpace(District))
            parts.Add($"р-н {District}");

        return parts.Any() ? string.Join(", ", parts) : string.Empty;
    }

    public string ToShortString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Street))
            parts.Add(Street);

        if (!string.IsNullOrWhiteSpace(Building))
            parts.Add(Building);

        if (!string.IsNullOrWhiteSpace(Apartment))
            parts.Add($"кв. {Apartment}");

        return parts.Any() ? string.Join(", ", parts) : string.Empty;
    }
}