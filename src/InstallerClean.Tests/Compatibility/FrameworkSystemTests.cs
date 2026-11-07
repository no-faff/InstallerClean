namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The enum, maths, array, range-check and platform members the app calls as .NET 10 has
/// them: an enum's declared values and whether a value is one, a value held between two
/// bounds in the overload of its own type, an array cleared or filled whole, a range check
/// that throws only past its bound, and the platform check.
/// </summary>
public class FrameworkSystemTests
{
    private enum Sample
    {
        Negative = -1,
        Two = 2,
        One = 1,
        AlsoOne = 1,
    }

    [Flags]
    private enum Options
    {
        None = 0,
        Read = 1,
        Write = 2,
        Both = 3,
        Delete = 4,
    }

    [Fact]
    public void GetValues_answers_every_declared_value_in_order_of_its_unsigned_value()
    {
        var values = Enum.GetValues<Sample>();

        Assert.Equal(new[] { Sample.One, Sample.AlsoOne, Sample.Two, Sample.Negative }, values);
    }

    [Fact]
    public void GetValues_answers_a_new_array_on_every_call()
    {
        var first = Enum.GetValues<Sample>();
        first[0] = Sample.Negative;

        Assert.Equal(Sample.One, Enum.GetValues<Sample>()[0]);
    }

    [Fact]
    public void IsDefined_is_true_for_a_declared_value_alone()
    {
        Assert.True(Enum.IsDefined(Sample.Negative));
        Assert.True(Enum.IsDefined((Sample)1));
        Assert.False(Enum.IsDefined((Sample)3));
    }

    [Fact]
    public void IsDefined_takes_a_combination_of_flags_only_where_a_name_declares_it()
    {
        Assert.True(Enum.IsDefined(Options.Read | Options.Write));
        Assert.False(Enum.IsDefined(Options.Read | Options.Delete));
    }

    [Fact]
    public void Clamp_gives_min_below_it_max_above_it_and_the_value_between_in_every_overload()
    {
        Assert.Equal(new byte[] { 2, 8, 5 }, new[] { Math.Clamp((byte)1, (byte)2, (byte)8), Math.Clamp((byte)9, (byte)2, (byte)8), Math.Clamp((byte)5, (byte)2, (byte)8) });
        Assert.Equal(new sbyte[] { -2, 8, 5 }, new[] { Math.Clamp((sbyte)-3, (sbyte)-2, (sbyte)8), Math.Clamp((sbyte)9, (sbyte)-2, (sbyte)8), Math.Clamp((sbyte)5, (sbyte)-2, (sbyte)8) });
        Assert.Equal(new short[] { -2, 8, 5 }, new[] { Math.Clamp((short)-3, (short)-2, (short)8), Math.Clamp((short)9, (short)-2, (short)8), Math.Clamp((short)5, (short)-2, (short)8) });
        Assert.Equal(new ushort[] { 2, 8, 5 }, new[] { Math.Clamp((ushort)1, (ushort)2, (ushort)8), Math.Clamp((ushort)9, (ushort)2, (ushort)8), Math.Clamp((ushort)5, (ushort)2, (ushort)8) });
        Assert.Equal(new[] { -2, 8, 5 }, new[] { Math.Clamp(-3, -2, 8), Math.Clamp(9, -2, 8), Math.Clamp(5, -2, 8) });
        Assert.Equal(new uint[] { 2, 8, 5 }, new[] { Math.Clamp(1u, 2u, 8u), Math.Clamp(9u, 2u, 8u), Math.Clamp(5u, 2u, 8u) });
        Assert.Equal(new long[] { -2, 8, 5 }, new[] { Math.Clamp(-3L, -2L, 8L), Math.Clamp(9L, -2L, 8L), Math.Clamp(5L, -2L, 8L) });
        Assert.Equal(new ulong[] { 2, 8, 5 }, new[] { Math.Clamp(1UL, 2UL, 8UL), Math.Clamp(9UL, 2UL, 8UL), Math.Clamp(5UL, 2UL, 8UL) });
        Assert.Equal(new nint[] { -2, 8, 5 }, new[] { Math.Clamp((nint)(-3), (nint)(-2), (nint)8), Math.Clamp((nint)9, (nint)(-2), (nint)8), Math.Clamp((nint)5, (nint)(-2), (nint)8) });
        Assert.Equal(new nuint[] { 2, 8, 5 }, new[] { Math.Clamp((nuint)1, (nuint)2, (nuint)8), Math.Clamp((nuint)9, (nuint)2, (nuint)8), Math.Clamp((nuint)5, (nuint)2, (nuint)8) });
        Assert.Equal(new[] { -0.5f, 8f, 5f }, new[] { Math.Clamp(-3f, -0.5f, 8f), Math.Clamp(9f, -0.5f, 8f), Math.Clamp(5f, -0.5f, 8f) });
        Assert.Equal(new[] { -0.5, 8, 5 }, new[] { Math.Clamp(-3.0, -0.5, 8.0), Math.Clamp(9.0, -0.5, 8.0), Math.Clamp(5.0, -0.5, 8.0) });
        Assert.Equal(new[] { -0.5m, 8m, 5m }, new[] { Math.Clamp(-3m, -0.5m, 8m), Math.Clamp(9m, -0.5m, 8m), Math.Clamp(5m, -0.5m, 8m) });
    }

    [Fact]
    public void Clamp_answers_in_the_type_of_its_arguments()
    {
        object fromLong = Math.Clamp(5L, 1, 8);
        object fromInt = Math.Clamp(5, 1, 8);
        object fromUint = Math.Clamp(5u, 1u, 8u);
        object fromDouble = Math.Clamp(5.0, 0, 1);

        Assert.IsType<long>(fromLong);
        Assert.IsType<int>(fromInt);
        Assert.IsType<uint>(fromUint);
        Assert.IsType<double>(fromDouble);
    }

    [Fact]
    public void Clamp_takes_a_bound_equal_to_the_value_and_bounds_equal_to_each_other()
    {
        Assert.Equal(-1L, Math.Clamp(-1L, -1L, int.MaxValue));
        Assert.Equal((long)int.MaxValue, Math.Clamp((long)int.MaxValue, -1L, int.MaxValue));
        Assert.Equal(4, Math.Clamp(9, 4, 4));
    }

    [Fact]
    public void Clamp_passes_NaN_through()
    {
        Assert.True(double.IsNaN(Math.Clamp(double.NaN, 0, 1)));
        Assert.True(float.IsNaN(Math.Clamp(float.NaN, 0f, 1f)));
    }

    [Fact]
    public void Clamp_gives_back_negative_zero_unchanged_against_a_lower_bound_of_zero()
    {
        var clamped = Math.Clamp(-0.0, 0.0, 1.0);

        Assert.Equal(BitConverter.DoubleToInt64Bits(-0.0), BitConverter.DoubleToInt64Bits(clamped));
    }

    [Fact]
    public void Clamp_with_min_above_max_throws_naming_both()
    {
        var thrown = Assert.Throws<ArgumentException>(() => Math.Clamp(5, 8, 2));

        Assert.Equal("'8' cannot be greater than 2.", thrown.Message);
        Assert.Null(thrown.ParamName);
    }

    [Fact]
    public void Clamp_with_min_above_max_throws_whatever_the_value()
    {
        Assert.Throws<ArgumentException>(() => Math.Clamp(double.NaN, 1.0, 0.0));
        Assert.Throws<ArgumentException>(() => Math.Clamp(5UL, 9UL, 1UL));
    }

    [Fact]
    public void Clear_sets_every_element_of_an_array_to_its_default()
    {
        var buffer = "{8B0C3F1A}".ToCharArray();

        Array.Clear(buffer);

        Assert.All(buffer, c => Assert.Equal('\0', c));
    }

    [Fact]
    public void Clear_clears_every_element_of_an_array_of_more_than_one_dimension()
    {
        var grid = new[,] { { 1, 2 }, { 3, 4 } };

        Array.Clear(grid);

        Assert.Equal(new[,] { { 0, 0 }, { 0, 0 } }, grid);
    }

    [Fact]
    public void Clear_clears_an_array_whose_index_does_not_start_at_zero()
    {
        var array = Array.CreateInstance(typeof(string), new[] { 3 }, new[] { 5 });
        array.SetValue("a", 5);
        array.SetValue("b", 7);

        Array.Clear(array);

        Assert.Null(array.GetValue(5));
        Assert.Null(array.GetValue(7));
    }

    [Fact]
    public void Clear_leaves_an_empty_array_as_it_is()
    {
        Array.Clear(Array.Empty<char>());
    }

    [Fact]
    public void Clear_refuses_a_null_array()
    {
        var thrown = Assert.Throws<ArgumentNullException>(() => Array.Clear(null!));

        Assert.Equal("array", thrown.ParamName);
    }

    [Fact]
    public void Fill_stores_the_value_in_every_element()
    {
        var answers = new (int, string?)[3];

        Array.Fill(answers, (7, "x"));

        Assert.Equal(new (int, string?)[] { (7, "x"), (7, "x"), (7, "x") }, answers);
    }

    [Fact]
    public void Fill_refuses_a_null_array()
    {
        var thrown = Assert.Throws<ArgumentNullException>(() => Array.Fill<int>(null!, 1));

        Assert.Equal("array", thrown.ParamName);
    }

    [Fact]
    public void Fill_refuses_a_value_an_array_of_a_derived_type_cannot_hold()
    {
        object[] strings = new string[2];

        Assert.Throws<ArrayTypeMismatchException>(() => Array.Fill(strings, new object()));
    }

    [Fact]
    public void ThrowIfLessThan_passes_a_value_equal_to_the_bound_or_above_it()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(-1L, -1L, "wait");
        ArgumentOutOfRangeException.ThrowIfLessThan(0L, -1L, "wait");
    }

    [Fact]
    public void ThrowIfLessThan_throws_below_the_bound_carrying_the_name_the_value_and_the_message()
    {
        var thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => ArgumentOutOfRangeException.ThrowIfLessThan(-2L, -1L, "wait"));

        Assert.Equal("wait", thrown.ParamName);
        Assert.Equal(-2L, thrown.ActualValue);
        Assert.StartsWith("wait ('-2') must be greater than or equal to '-1'.", thrown.Message);
    }

    [Fact]
    public void ThrowIfGreaterThan_passes_a_value_equal_to_the_bound_or_below_it()
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((long)int.MaxValue, int.MaxValue, "wait");
        ArgumentOutOfRangeException.ThrowIfGreaterThan(0L, int.MaxValue, "wait");
    }

    [Fact]
    public void ThrowIfGreaterThan_throws_above_the_bound_carrying_the_name_the_value_and_the_message()
    {
        var thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => ArgumentOutOfRangeException.ThrowIfGreaterThan(2147483648L, int.MaxValue, "wait"));

        Assert.Equal("wait", thrown.ParamName);
        Assert.Equal(2147483648L, thrown.ActualValue);
        Assert.StartsWith("wait ('2147483648') must be less than or equal to '2147483647'.", thrown.Message);
    }

    [Fact]
    public void The_range_checks_name_the_argument_as_written_where_no_name_is_given()
    {
        var count = -1;

        var less = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentOutOfRangeException.ThrowIfLessThan(count, 0));
        var greater = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentOutOfRangeException.ThrowIfGreaterThan(count + 2, 0));

        Assert.Equal("count", less.ParamName);
        Assert.StartsWith("count ('-1') must be greater than or equal to '0'.", less.Message);
        Assert.Equal("count + 2", greater.ParamName);
    }

    [Fact]
    public void IsWindows_is_true()
    {
        Assert.True(OperatingSystem.IsWindows());
    }
}
