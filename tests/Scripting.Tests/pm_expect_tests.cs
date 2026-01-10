using FluentAssertions;
using Scripting.Api;

namespace Scripting.Tests;

public class pm_expect_tests
{
    [Fact]
    public void equal_passes_when_values_match()
    {
        var expect = new pm_expect(42);

        var action = () => expect.to.equal(42);

        action.Should().NotThrow();
    }

    [Fact]
    public void equal_throws_when_values_do_not_match()
    {
        var expect = new pm_expect(42);

        var action = () => expect.to.equal(100);

        action.Should().Throw<Exception>().WithMessage("*Expected '100' but got '42'*");
    }

    [Fact]
    public void ok_passes_when_value_is_truthy()
    {
        var expect = new pm_expect(true);

        var action = () => expect.to.be.ok();

        action.Should().NotThrow();
    }

    [Fact]
    public void ok_throws_when_value_is_falsy()
    {
        var expect = new pm_expect(false);

        var action = () => expect.to.be.ok();

        action.Should().Throw<Exception>();
    }

    [Fact]
    public void true_passes_when_value_is_true()
    {
        var expect = new pm_expect(true);

        var action = () => expect.to.be.@true();

        action.Should().NotThrow();
    }

    [Fact]
    public void false_passes_when_value_is_false()
    {
        var expect = new pm_expect(false);

        var action = () => expect.to.be.@false();

        action.Should().NotThrow();
    }

    [Fact]
    public void null_passes_when_value_is_null()
    {
        var expect = new pm_expect(null);

        var action = () => expect.to.be.@null();

        action.Should().NotThrow();
    }

    [Fact]
    public void include_passes_when_string_contains_substring()
    {
        var expect = new pm_expect("hello world");

        var action = () => expect.to.include("world");

        action.Should().NotThrow();
    }

    [Fact]
    public void include_throws_when_string_does_not_contain_substring()
    {
        var expect = new pm_expect("hello world");

        var action = () => expect.to.include("foo");

        action.Should().Throw<Exception>().WithMessage("*to include 'foo'*");
    }

    [Fact]
    public void above_passes_when_value_is_greater()
    {
        var expect = new pm_expect(100);

        var action = () => expect.to.be.above(50);

        action.Should().NotThrow();
    }

    [Fact]
    public void above_throws_when_value_is_not_greater()
    {
        var expect = new pm_expect(50);

        var action = () => expect.to.be.above(100);

        action.Should().Throw<Exception>();
    }

    [Fact]
    public void below_passes_when_value_is_less()
    {
        var expect = new pm_expect(50);

        var action = () => expect.to.be.below(100);

        action.Should().NotThrow();
    }

    [Fact]
    public void length_passes_when_string_has_expected_length()
    {
        var expect = new pm_expect("hello");

        var action = () => expect.to.have.length(5);

        action.Should().NotThrow();
    }

    [Fact]
    public void not_equal_passes_when_values_differ()
    {
        var expect = new pm_expect(42);

        var action = () => expect.not.to.equal(100);

        action.Should().NotThrow();
    }

    [Fact]
    public void not_equal_throws_when_values_match()
    {
        var expect = new pm_expect(42);

        var action = () => expect.not.to.equal(42);

        action.Should().Throw<Exception>();
    }

    [Fact]
    public void oneOf_passes_when_value_is_in_list()
    {
        var expect = new pm_expect(200);

        var action = () => expect.to.be.oneOf(200, 201, 204);

        action.Should().NotThrow();
    }

    [Fact]
    public void oneOf_throws_when_value_is_not_in_list()
    {
        var expect = new pm_expect(500);

        var action = () => expect.to.be.oneOf(200, 201, 204);

        action.Should().Throw<Exception>();
    }
}
