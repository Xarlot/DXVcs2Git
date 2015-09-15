using System.Collections.Generic;
using DXVcs2Git.Core;
using NUnit.Framework;
using Polenter.Serialization;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class UsersConfigGenerator {
        IEnumerable<User> GenerateUsers() {
            List<User> list = new List<User>();
            list.Add(new User("DXVcs2GitService", "noreply@mail.com"));
            list.Add(new User("Bobyshev.Alexander", "noreply@mail.com"));
            list.Add(new User("Dementyev.Andrey", "andrey.dementyev@devexpress.com"));
            list.Add(new User("ershov.mikhail", "mikhail.ershov@devexpress.com"));
            list.Add(new User("Eskin", "mikhail.eskin@devexpress.com"));
            list.Add(new User("filatov.denis", "noreply@devexpress.com"));
            list.Add(new User("filinov.vsevolod", "vsevolod.filinov@devexpress.com"));
            list.Add(new User("khmelnitsky", "denis.khmelnitsky@devexpress.com"));
            list.Add(new User("litvinov", "maxim.litvinov@devexpress.com"));
            list.Add(new User("Komarov", "alexander.komarov@devexpress.com"));
            list.Add(new User("Serov.Alexey", "alexey.serov@devexpress.com"));
            list.Add(new User("rebrov.artemp", "artemp.rebrov@devexpress.com"));
            list.Add(new User("Abanin.Anton", "anton.abanin@devexpress.com"));
            list.Add(new User("Egorov", "alexander.egorov@devexpress.com"));
            list.Add(new User("kostikov.ilya", "ilya.kostikov@devexpress.com"));
            return list;
        }

        [Test, Explicit]
        public void Generate() {
            RegisteredUsers users = new RegisteredUsers(GenerateUsers());
            SharpSerializer serializer = new SharpSerializer(new SharpSerializerXmlSettings() { IncludeAssemblyVersionInTypeName = false, IncludePublicKeyTokenInTypeName = false});
            serializer.Serialize(users, @"c:\users.config");
        }
    }
}
