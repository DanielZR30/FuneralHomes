namespace FH.Financials.Domain.Enums;

/// <summary>Parentesco del beneficiario con el titular (alineado con el microservicio Customers).</summary>
public enum RelationshipType
{
    Principal = 1,
    Spouse = 2,
    Child = 3,
    Parent = 4,
    Employee = 5,
    Pet = 6,
    Other = 7
}
