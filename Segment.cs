using Arc.IO;

namespace Arc
{
    public abstract class Segment
    {
        public abstract void Read(DataReader reader);
    }
}