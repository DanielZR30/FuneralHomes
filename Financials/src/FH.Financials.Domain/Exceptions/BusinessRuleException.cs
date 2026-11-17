namespace FH.Financials.Domain.Exceptions;

/// <summary>Regla de negocio incumplida (se traduce a HTTP 422, igual que en Customers).</summary>
public class BusinessRuleException(string message) : Exception(message);
