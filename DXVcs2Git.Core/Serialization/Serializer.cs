using System.IO;
using Polenter.Serialization;

namespace DXVcs2Git.Core {
    public static class Serializer {
        public static void Serialize<T>(string path, T value) {
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(value, path);
        }
        public static void Serialize<T>(Stream stream, T value) {
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(value, stream);
        }
        public static T Deserialize<T>(string path) {
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            return (T)serializer.Deserialize(path);
        }
        public static T Deserialize<T>(Stream stream) {
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            return (T)serializer.Deserialize(stream);
        }
    }
}
