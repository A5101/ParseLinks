using Parse.Service;

namespace SearchApi
{
    public static class GloveInstance
    {
        private static readonly Lazy<Glove> _glove = new Lazy<Glove>(() =>
        {
            return new Glove(modelPath: @"..\Parse\bin\Debug\net7.0\data.json");
        });

        public static Glove Instance => _glove.Value;
    }
}
