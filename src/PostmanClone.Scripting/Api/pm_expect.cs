namespace PostmanClone.Scripting.Api;

public class pm_expect
{
    private readonly object? _actual;

    public pm_expect(object? actual)
    {
        _actual = actual;
    }

    public pm_expect to => this;
    public pm_expect be => this;
    public pm_expect have => this;
    public pm_expect a => this;
    public pm_expect an => this;
    public pm_expect_negated not => new pm_expect_negated(_actual);

    public void equal(object? expected)
    {
        if (!Equals(_actual, expected))
        {
            throw new Exception($"Expected '{expected}' but got '{_actual}'");
        }
    }

    public void eql(object? expected) => equal(expected);

    public void ok()
    {
        if (_actual is null || Equals(_actual, false) || Equals(_actual, 0) || Equals(_actual, ""))
        {
            throw new Exception($"Expected truthy value but got '{_actual}'");
        }
    }

    public void @true()
    {
        if (!Equals(_actual, true))
        {
            throw new Exception($"Expected true but got '{_actual}'");
        }
    }

    public void @false()
    {
        if (!Equals(_actual, false))
        {
            throw new Exception($"Expected false but got '{_actual}'");
        }
    }

    public void @null()
    {
        if (_actual is not null)
        {
            throw new Exception($"Expected null but got '{_actual}'");
        }
    }

    public void undefined()
    {
        @null();
    }

    public void empty()
    {
        if (_actual is string s && s.Length > 0)
        {
            throw new Exception($"Expected empty string but got '{_actual}'");
        }
        if (_actual is System.Collections.ICollection c && c.Count > 0)
        {
            throw new Exception($"Expected empty collection but got {c.Count} items");
        }
    }

    public void include(object? item)
    {
        if (_actual is string s && item is string sub)
        {
            if (!s.Contains(sub))
            {
                throw new Exception($"Expected '{s}' to include '{sub}'");
            }
            return;
        }
        throw new Exception($"Include check not supported for type {_actual?.GetType().Name}");
    }

    public void contain(object? item) => include(item);

    public void above(double expected)
    {
        var actual_num = Convert.ToDouble(_actual);
        if (actual_num <= expected)
        {
            throw new Exception($"Expected {actual_num} to be above {expected}");
        }
    }

    public void below(double expected)
    {
        var actual_num = Convert.ToDouble(_actual);
        if (actual_num >= expected)
        {
            throw new Exception($"Expected {actual_num} to be below {expected}");
        }
    }

    public void at_least(double expected)
    {
        var actual_num = Convert.ToDouble(_actual);
        if (actual_num < expected)
        {
            throw new Exception($"Expected {actual_num} to be at least {expected}");
        }
    }

    public void at_most(double expected)
    {
        var actual_num = Convert.ToDouble(_actual);
        if (actual_num > expected)
        {
            throw new Exception($"Expected {actual_num} to be at most {expected}");
        }
    }

    public void length(int expected)
    {
        int actual_length = 0;
        if (_actual is string s) actual_length = s.Length;
        else if (_actual is System.Collections.ICollection c) actual_length = c.Count;
        else throw new Exception($"Cannot get length of {_actual?.GetType().Name}");

        if (actual_length != expected)
        {
            throw new Exception($"Expected length {expected} but got {actual_length}");
        }
    }

    public void property(string name)
    {
        if (_actual is null)
        {
            throw new Exception($"Expected object to have property '{name}' but object is null");
        }
        var prop = _actual.GetType().GetProperty(name);
        if (prop is null)
        {
            throw new Exception($"Expected object to have property '{name}'");
        }
    }

    public void status(int expected_code)
    {
        equal(expected_code);
    }

    public void oneOf(params object[] values)
    {
        if (!values.Contains(_actual))
        {
            throw new Exception($"Expected '{_actual}' to be one of [{string.Join(", ", values)}]");
        }
    }
}

public class pm_expect_negated
{
    private readonly object? _actual;

    public pm_expect_negated(object? actual)
    {
        _actual = actual;
    }

    public pm_expect_negated to => this;
    public pm_expect_negated be => this;

    public void equal(object? expected)
    {
        if (Equals(_actual, expected))
        {
            throw new Exception($"Expected '{_actual}' to not equal '{expected}'");
        }
    }

    public void eql(object? expected) => equal(expected);

    public void ok()
    {
        if (_actual is not null && !Equals(_actual, false) && !Equals(_actual, 0) && !Equals(_actual, ""))
        {
            throw new Exception($"Expected falsy value but got '{_actual}'");
        }
    }

    public void @null()
    {
        if (_actual is null)
        {
            throw new Exception("Expected value to not be null");
        }
    }

    public void empty()
    {
        if (_actual is string s && s.Length == 0)
        {
            throw new Exception("Expected non-empty string");
        }
        if (_actual is System.Collections.ICollection c && c.Count == 0)
        {
            throw new Exception("Expected non-empty collection");
        }
    }

    public void include(object? item)
    {
        if (_actual is string s && item is string sub)
        {
            if (s.Contains(sub))
            {
                throw new Exception($"Expected '{s}' to not include '{sub}'");
            }
            return;
        }
    }

    public void contain(object? item) => include(item);
}
