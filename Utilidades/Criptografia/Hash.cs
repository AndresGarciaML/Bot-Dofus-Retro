﻿using System;
using System.Text;

namespace Bot_Dofus_1._29._1.Utilidades.Criptografia
{
    public class Hash
    {
        public static char[] caracteres_array = new char[]
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F',
            'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
            'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_'
        };

        public static char[] HEX_CHARS = new char[]
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
        };

        public static string encriptar_Password(string password, string key)
        {
            StringBuilder str = new StringBuilder().Append("#1");
            for (int i = 0; i < password.Length; i++)
            {
                char ch = password[i];
                char ch2 = key[i];
                int num2 = ch / 16;
                int num3 = ch % 16;
                int index = (num2 + ch2) % caracteres_array.Length;
                int num5 = (num3 + ch2) % caracteres_array.Length;
                str.Append(caracteres_array[index]).Append(caracteres_array[num5]);
            }
            return str.ToString();
        }

        public static string desencriptar_Ip(string paquete)
        {
            StringBuilder ip = new StringBuilder();

            for (int i = 0; i < 8; i += 2)
            {
                int ascii1 = paquete[i] - 48;
                int ascii2 = paquete[i + 1] - 48;
                
                if (i != 0)
                    ip.Append('.');

                ip.Append(((ascii1 & 15) << 4) | (ascii2 & 15));
            }
            return ip.ToString();
        }

        public static int desencriptar_Puerto(char[] chars)
        {
            if (chars.Length != 3)
                throw new ArgumentOutOfRangeException("El puerto debe estar encriptado en 3 caracteres.");

            int puerto = 0;
            for (int i = 0; i < 2; i++)
                puerto += (int)(Math.Pow(64, 2 - i) * get_Hash(chars[i]));

            puerto += get_Hash(chars[2]);
            return puerto;
        }

        public static int get_Hash(char ch)
        {
            for (int i = 0; i < caracteres_array.Length; i++)
                if (caracteres_array[i] == ch)
                    return i;

            throw new IndexOutOfRangeException(ch + " no esta en el array del hash");
        }

        public static string encriptar_hexadecimal(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
                sb.AppendFormat("{0:X2}", (int)c);
            return sb.ToString().Trim();
        }
    }
}
