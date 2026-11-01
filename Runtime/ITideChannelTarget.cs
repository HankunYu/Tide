namespace Tide
{
    /// <summary>
    /// A component a channel track can drive: named channels, each holding
    /// one <see cref="TideValueType"/>, read and written one at a time. The
    /// type decides how clips mix on the channel: continuous values
    /// interpolate, discrete ones (Int, Bool, Gradient) switch.
    /// </summary>
    public interface ITideChannelTarget
    {
        int ChannelCount { get; }

        string GetChannelName(int index);

        TideValueType GetChannelType(int index);

        TideValue GetChannelValue(int index);

        /// <summary>Sets the channel; <paramref name="value"/> is of the channel's type.</summary>
        void SetChannelValue(int index, TideValue value);
    }
}
