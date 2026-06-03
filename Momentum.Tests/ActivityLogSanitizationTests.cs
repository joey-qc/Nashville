using Momentum.Application.Services;
using System.ComponentModel.DataAnnotations;
using Momentum.Shared;

namespace Momentum.Tests;

public class ActivityLogSanitizationTests
{
    // ── Disallowed tags stripped ───────────────────────────────────────────

    [Fact]
    public void SanitizeNotes_StripsScriptTags()
    {
        // Security property: no executable <script> tag survives. With KeepChildNodes=true,
        // a crafted payload's inert TEXT may remain — that is harmless because it is never
        // rendered as markup (no script element is created) and the editor never produces
        // <script> tags (paste is stripped to plain text).
        var result = ActivityLogService.SanitizeNotes("<script>alert('xss')</script>Hello");
        Assert.NotNull(result);
        Assert.DoesNotContain("<script", result);
        Assert.DoesNotContain("</script", result);
        Assert.Contains("Hello", result);
    }

    [Fact]
    public void SanitizeNotes_StripsAnchorTag_KeepsText()
    {
        // <a> is not allowlisted; KeepChildNodes=true unwraps it, preserving the link text
        // as plain text. The security goal — no <a> element and no href in storage — is met.
        var result = ActivityLogService.SanitizeNotes("<a href=\"https://evil.com\">click me</a>");
        Assert.NotNull(result);
        Assert.DoesNotContain("<a", result);
        Assert.DoesNotContain("href", result);
        Assert.Contains("click me", result);
    }

    [Fact]
    public void SanitizeNotes_StripsImgTags()
    {
        var result = ActivityLogService.SanitizeNotes("<img src=\"x\" onerror=\"alert(1)\">notes");
        Assert.NotNull(result);
        Assert.DoesNotContain("<img", result);
        Assert.DoesNotContain("onerror", result);
    }

    [Fact]
    public void SanitizeNotes_StripsAllAttributes()
    {
        var result = ActivityLogService.SanitizeNotes("<p class=\"evil\" style=\"color:red\">text</p>");
        Assert.NotNull(result);
        Assert.DoesNotContain("class=", result);
        Assert.DoesNotContain("style=", result);
        Assert.Contains("<p>", result);
    }

    // ── Allowed formatting preserved ──────────────────────────────────────

    [Fact]
    public void SanitizeNotes_PreservesBoldTag()
    {
        var result = ActivityLogService.SanitizeNotes("<strong>bold text</strong>");
        Assert.Equal("<strong>bold text</strong>", result);
    }

    [Fact]
    public void SanitizeNotes_PreservesItalicTag()
    {
        var result = ActivityLogService.SanitizeNotes("<em>italic text</em>");
        Assert.Equal("<em>italic text</em>", result);
    }

    [Fact]
    public void SanitizeNotes_PreservesUnderlineTag()
    {
        var result = ActivityLogService.SanitizeNotes("<u>underlined</u>");
        Assert.Equal("<u>underlined</u>", result);
    }

    [Fact]
    public void SanitizeNotes_PreservesBulletList()
    {
        var html = "<ul><li>item one</li><li>item two</li></ul>";
        var result = ActivityLogService.SanitizeNotes(html);
        Assert.NotNull(result);
        Assert.Contains("<ul>", result);
        Assert.Contains("<li>item one</li>", result);
        Assert.Contains("<li>item two</li>", result);
    }

    [Fact]
    public void SanitizeNotes_PreservesDivWrappedList()
    {
        // Regression for Bug 3: when text precedes a bullet list, the browser's
        // execCommand wraps the list in a <div>. The <div> is not allowlisted; with
        // KeepChildNodes=true it is unwrapped but the <ul>/<li> inside must survive.
        var html   = "Para text<div><ul><li>bullet one</li><li>bullet two</li></ul></div>";
        var result = ActivityLogService.SanitizeNotes(html);
        Assert.NotNull(result);
        Assert.Contains("Para text", result);
        Assert.Contains("<ul>", result);
        Assert.Contains("<li>bullet one</li>", result);
        Assert.Contains("<li>bullet two</li>", result);
        Assert.DoesNotContain("<div", result);
    }

    [Fact]
    public void SanitizeNotes_PreservesTextThenList()
    {
        // Bare (un-wrapped) text-then-list also survives
        var html   = "Walked today<ul><li>3 miles</li></ul>";
        var result = ActivityLogService.SanitizeNotes(html);
        Assert.NotNull(result);
        Assert.Contains("Walked today", result);
        Assert.Contains("<li>3 miles</li>", result);
    }

    [Fact]
    public void SanitizeNotes_PreservesBoldBTag()
    {
        // execCommand("bold") produces <b> in Chrome; must survive sanitization
        var result = ActivityLogService.SanitizeNotes("<b>bold via execCommand</b>");
        Assert.NotNull(result);
        Assert.Contains("<b>", result);
    }

    [Fact]
    public void SanitizeNotes_PreservesItalicITag()
    {
        // execCommand("italic") produces <i> in Chrome
        var result = ActivityLogService.SanitizeNotes("<i>italic via execCommand</i>");
        Assert.NotNull(result);
        Assert.Contains("<i>", result);
    }

    [Fact]
    public void SanitizeNotes_PreservesParagraphTag()
    {
        var result = ActivityLogService.SanitizeNotes("<p>first</p><p>second</p>");
        Assert.NotNull(result);
        Assert.Contains("<p>first</p>", result);
        Assert.Contains("<p>second</p>", result);
    }

    // ── Blank HTML normalized to NULL ─────────────────────────────────────

    [Fact]
    public void SanitizeNotes_ReturnsNull_ForNull()
    {
        Assert.Null(ActivityLogService.SanitizeNotes(null));
    }

    [Fact]
    public void SanitizeNotes_ReturnsNull_ForEmptyString()
    {
        Assert.Null(ActivityLogService.SanitizeNotes(""));
    }

    [Fact]
    public void SanitizeNotes_ReturnsNull_ForWhitespaceOnly()
    {
        Assert.Null(ActivityLogService.SanitizeNotes("   "));
    }

    [Fact]
    public void SanitizeNotes_ReturnsNull_ForEmptyParagraph()
    {
        Assert.Null(ActivityLogService.SanitizeNotes("<p></p>"));
    }

    [Fact]
    public void SanitizeNotes_ReturnsNull_ForEmptyParagraphWithBr()
    {
        Assert.Null(ActivityLogService.SanitizeNotes("<p><br></p>"));
    }

    [Fact]
    public void SanitizeNotes_ReturnsNull_ForNbspOnly()
    {
        Assert.Null(ActivityLogService.SanitizeNotes("&nbsp;"));
    }

    [Fact]
    public void SanitizeNotes_ReturnsNonNull_ForRealContent()
    {
        var result = ActivityLogService.SanitizeNotes("<p>Great workout today.</p>");
        Assert.NotNull(result);
        Assert.Contains("Great workout today.", result);
    }

    // ── DTO MaxLength validation ───────────────────────────────────────────

    [Fact]
    public void CreateActivityLogDto_Accepts10000CharNotes()
    {
        var dto = new CreateActivityLogDto
        {
            ActivityId     = 1,
            LoggedAt       = DateTime.UtcNow,
            PointsRecorded = 5,
            Notes          = new string('a', 10000)
        };
        var results = new List<ValidationResult>();
        var valid   = Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        Assert.True(valid, string.Join("; ", results.Select(r => r.ErrorMessage)));
    }

    [Fact]
    public void CreateActivityLogDto_Rejects10001CharNotes()
    {
        var dto = new CreateActivityLogDto
        {
            ActivityId     = 1,
            LoggedAt       = DateTime.UtcNow,
            PointsRecorded = 5,
            Notes          = new string('a', 10001)
        };
        var results = new List<ValidationResult>();
        var valid   = Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        Assert.False(valid);
    }

    [Fact]
    public void UpdateActivityLogDto_Accepts10000CharNotes()
    {
        var dto = new UpdateActivityLogDto
        {
            ActivityId     = 1,
            LoggedAt       = DateTime.UtcNow,
            PointsRecorded = 5,
            Notes          = new string('a', 10000)
        };
        var results = new List<ValidationResult>();
        var valid   = Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        Assert.True(valid, string.Join("; ", results.Select(r => r.ErrorMessage)));
    }
}
