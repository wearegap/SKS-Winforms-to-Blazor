using System.Collections.Specialized;

namespace SKS_Blazor.Application.Services.Functions;

/// <summary>Port of <c>modFunctions</c> — shared utility functions used across forms.</summary>
public interface IFunctionsService
{
    /// <summary>
    /// Port of <c>modFunctions.AddToCollection</c>.
    /// Adds an item to the OrderedDictionary if it does not already exist.
    /// Returns true if added, false if already present.
    /// </summary>
    Task<bool> AddToCollectionAsync(OrderedDictionary col, string Item, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>modFunctions.UpCase</c>.
    /// If the given ASCII code represents a lowercase letter (97–122),
    /// returns the uppercase equivalent ASCII code; otherwise returns it unchanged.
    /// </summary>
    Task<int> UpCaseAsync(int KeyAscii, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>modFunctions.RcrdId</c>.
    /// Generates the next record identifier for the specified table by reading the max
    /// value of the specified field, incrementing by 1, and optionally prefixing with
    /// an Identifier string and suffixing with the current month.
    /// </summary>
    Task<string?> RcrdIdAsync(string Table, string Identifier, string FldNo, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>modFunctions.ExecErr</c>.
    /// Builds an error message that includes related records from the specified table
    /// matching the given field/value, or returns a plain prompt if Table is empty.
    /// </summary>
    Task<string?> ExecErrAsync(string Prompt, string PromptFld, string Table, string RcrdFld, string RcrdStr, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>modFunctions.AppendAND</c>.
    /// Appends " AND " to the criteria string if it already has content.
    /// </summary>
    string AppendAND(string criteria);

    /// <summary>
    /// Port of <c>modFunctions.Exists</c>.
    /// Checks if a key exists in an OrderedDictionary.
    /// </summary>
    bool Exists(OrderedDictionary col, string Index);

    /// <summary>
    /// Port of <c>modFunctions.DoubleValue</c>.
    /// Parses a string to double, returning 0 if empty or invalid.
    /// </summary>
    double DoubleValue(string strValue);

    /// <summary>
    /// Port of <c>modFunctions.ValidateTextBoxDouble</c>.
    /// Validates that a text value is a valid double (or empty). Returns true if valid.
    /// </summary>
    bool ValidateTextBoxDouble(string text);

    /// <summary>
    /// Port of <c>modFunctions.ValidateTextDouble</c>.
    /// Validates that a text value is a valid double. Returns true if valid.
    /// </summary>
    bool ValidateTextDouble(string text);

    /// <summary>
    /// Port of <c>modFunctions.SelectAll</c>.
    /// Returns a SELECT * FROM {table} SQL string (used by callers that build criteria).
    /// </summary>
    string SelectAll(string table);

    /// <summary>
    /// Port of <c>modFunctions.LoadCombo</c>.
    /// Loads distinct values from a table/field into a list for combo population.
    /// </summary>
    Task<List<string>> LoadComboAsync(string table, string field, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>modFunctions.ComboEmpty</c>.
    /// Checks if a combo selection text is empty or null.
    /// </summary>
    bool ComboEmpty(string? comboText);

    /// <summary>
    /// Port of <c>modFunctions.NoRecords</c>.
    /// Returns a standard "No records found" message.
    /// </summary>
    string NoRecords();

    /// <summary>
    /// Port of <c>modFunctions.SearchShow</c>.
    /// Builds a search criteria SQL fragment for the given field, value, and search type.
    /// </summary>
    string SearchShow(string field, string value, string searchType);

    /// <summary>
    /// Port of <c>modFunctions.ValBox</c>.
    /// Validates that a text value is not empty. Returns error message or empty string.
    /// </summary>
    string ValBox(string text, string fieldName);

    /// <summary>
    /// Port of <c>modFunctions.TextBoxEmpty</c>.
    /// Checks if a text field is empty or whitespace.
    /// </summary>
    bool TextBoxEmpty(string? text);

    /// <summary>
    /// Port of <c>modFunctions.TextBoxNumberEmpty</c>.
    /// Checks if a numeric text field is empty, whitespace, or zero.
    /// </summary>
    bool TextBoxNumberEmpty(string? text);
}
