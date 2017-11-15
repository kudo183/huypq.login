using System.Collections.Generic;

namespace huypq.login.Helper
{
    public static class ListStringHelper
    {
        public static byte[] ToByteArray(List<string> textData)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var bw = new System.IO.BinaryWriter(ms))
            {
                bw.Write(textData.Count);
                for (int i = 0; i < textData.Count; i++)
                {
                    bw.Write(textData[i]);
                }
                bw.Flush();
                return ms.ToArray();
            }
        }

        public static List<string> FromByteArray(byte[] bytes)
        {
            using (var ms = new System.IO.MemoryStream(bytes))
            using (var br = new System.IO.BinaryReader(ms))
            {
                var count = br.ReadInt32();

                var result = new List<string>(count);
                for (int i = 0; i < count; i++)
                {
                    result.Add(br.ReadString());
                }
                return result;
            }
        }
    }
}
