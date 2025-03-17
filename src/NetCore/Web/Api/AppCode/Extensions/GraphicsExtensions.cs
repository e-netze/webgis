#pragma warning disable CA1416

using E.Standard.Extensions.Credentials;
using gView.GraphicsEngine;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Api.Core.AppCode.Extensions;

static public class GraphicsExtensions
{
    static public ArgbColor ColorFromUsername(this string username)
    {
        username = username.PureUsername().ToLower();

        if (username.Length == 0)
        {
            username = "unknown";
        }

        while (username.Length < 5)
        {
            username += username;
        }

        byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
        usernameBytes = SHA256.Create().ComputeHash(usernameBytes);
        username = Convert.ToBase64String(usernameBytes);

        var hashedUsernameBytes = System.Text.Encoding.UTF8.GetBytes(username).ToArray();

        var bytes = new byte[3];
        for (int i = 0; i < hashedUsernameBytes.Length; i++)
        {
            bytes[i % 3] = (byte)((bytes[i % 3] + hashedUsernameBytes[i]) % 256);
        }

        var max = bytes.Max();
        for (int i = 0; i < 3; i++)
        {
            if (bytes[i] == max)
            {
                bytes[i] = 255;
                break;
            }
        }

        string hex = BitConverter.ToString(bytes).Replace("-", "");

        return ("#" + hex.Substring(0, 6)).HexToColor();
    }

    static public ArgbColor HexToColor(this string hex, float opacity = 1f)
    {

        if (String.IsNullOrWhiteSpace(hex) || hex.ToLower().Trim() == "none")
        {
            return ArgbColor.Transparent;
        }

        if (!hex.StartsWith("#"))
        {
            hex = $"#{hex}";
        }

        var color = ArgbColor.FromHexString(hex);
        if (opacity >= 0.0 && opacity < 1.0)
        {
            color = ArgbColor.FromArgb((int)(opacity * 255f), color);
        }

        return color;
    }

    static public LineDashStyle ToLineStyle(this string style)
    {
        switch (style)
        {
            case "10,20":
                return LineDashStyle.Dash;
            case "15,15,3,15":
                return LineDashStyle.DashDot;
            case "15,15,3,15,3,15":
                return LineDashStyle.DashDotDot;
            case "3,15":
                return LineDashStyle.Dot;
        }

        return LineDashStyle.Solid;
    }
}
