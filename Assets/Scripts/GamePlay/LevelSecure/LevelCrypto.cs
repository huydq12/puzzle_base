using System;
using System.IO;
using System.Security.Cryptography;

public static class LevelCrypto
{
    private const string Magic = "LVL1";

    private static readonly byte[] _encKey = new byte[32]
    {
        0x71, 0xC3, 0x19, 0x2B, 0x9A, 0x4D, 0xE0, 0x66,
        0x55, 0xAF, 0x03, 0xD1, 0x8E, 0x22, 0xC7, 0xB8,
        0x11, 0x6A, 0x9D, 0xF2, 0x3C, 0x80, 0x47, 0x5E,
        0x0B, 0xF9, 0xD4, 0x28, 0x6C, 0xA1, 0x73, 0x9E
    };

    private static readonly byte[] _macKey = new byte[32]
    {
        0x2F, 0x91, 0xAD, 0x04, 0x6B, 0x77, 0xD2, 0x1C,
        0xE6, 0x38, 0x9B, 0x50, 0x0D, 0xCA, 0x14, 0x8A,
        0xF1, 0x63, 0x2E, 0x99, 0x70, 0xB4, 0xC8, 0x05,
        0xDE, 0x41, 0x87, 0x3A, 0x1F, 0xBC, 0x56, 0x20
    };

    public static byte[] EncryptAndSign(byte[] plaintext)
    {
        if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));

        byte[] iv = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        byte[] ciphertext;
        using (var aes = Aes.Create())
        {
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = _encKey;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(Magic);
            bw.Write((byte)1);
            bw.Write((byte)iv.Length);
            bw.Write(iv);
            bw.Write(ciphertext.Length);
            bw.Write(ciphertext);
        }

        byte[] payload = ms.ToArray();
        byte[] tag;
        using (var hmac = new HMACSHA256(_macKey))
        {
            tag = hmac.ComputeHash(payload);
        }

        using var ms2 = new MemoryStream();
        using (var bw2 = new BinaryWriter(ms2))
        {
            bw2.Write(payload.Length);
            bw2.Write(payload);
            bw2.Write((byte)tag.Length);
            bw2.Write(tag);
        }

        return ms2.ToArray();
    }

    public static bool TryVerifyAndDecrypt(byte[] fileBytes, out byte[] plaintext)
    {
        plaintext = null;
        if (fileBytes == null || fileBytes.Length < 8) return false;

        try
        {
            using var ms = new MemoryStream(fileBytes);
            using var br = new BinaryReader(ms);

            int payloadLen = br.ReadInt32();
            if (payloadLen <= 0 || payloadLen > fileBytes.Length - 6) return false;

            byte[] payload = br.ReadBytes(payloadLen);
            int tagLen = br.ReadByte();
            if (tagLen <= 0 || tagLen > 64) return false;
            byte[] tag = br.ReadBytes(tagLen);
            if (tag.Length != tagLen) return false;

            using (var hmac = new HMACSHA256(_macKey))
            {
                byte[] expected = hmac.ComputeHash(payload);
                if (!CryptographicOperations.FixedTimeEquals(expected, tag))
                {
                    return false;
                }
            }

            using var msPayload = new MemoryStream(payload);
            using var br2 = new BinaryReader(msPayload);

            string magic = br2.ReadString();
            if (!string.Equals(magic, Magic, StringComparison.Ordinal)) return false;

            byte version = br2.ReadByte();
            if (version != 1) return false;

            int ivLen = br2.ReadByte();
            if (ivLen != 16) return false;

            byte[] iv = br2.ReadBytes(ivLen);
            int cipherLen = br2.ReadInt32();
            if (cipherLen <= 0) return false;
            byte[] cipher = br2.ReadBytes(cipherLen);
            if (cipher.Length != cipherLen) return false;

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = _encKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            plaintext = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return plaintext != null;
        }
        catch
        {
            plaintext = null;
            return false;
        }
    }
}
