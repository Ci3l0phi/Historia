using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Historia
{
    public class Decryptor
    {
        private readonly int index = 11;
        private readonly string[] keys =
        {
            "jvwvqgfghyrqewea",
            "gsqsdafkssqshafd",
            "mmxi0dozaqblnmwo",
            "nyrqvwescuanhanu",
            "gbeugfaacudgaxcs",
            "rbneolithqb5yswm",
            "1udgandceoferaxe",
            "qarqvwessdicaqbe",
            "2udqaxxxiscogito",
            "affcbjwxymsi0doz",
            "ydigadeath8nnmwd",
            "hsunffalqyrqewes",
            "gqbnnmnmymsi0doz",
            "hvwvqgfl1nrqewes",
            "sxqsdafkssqsdafk",
            "ssqsaafsssqsdof6",
        };

        public void Decrypt(byte[] buffer, int length, int offset = 0)
        {
            var blowfish = new Blowfish(new uint[1024], this.keys[this.index]);
            blowfish.Decipher(buffer, offset, length);
        }
    }
}
