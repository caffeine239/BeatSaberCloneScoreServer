using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


public class NetworkSession
{
    public Socket AuthSocket;
    byte[] DataBuffer;

    void HandleAuthData(byte[] data)
    {
        try
        {
            for (int index = 0; index < data.Length; index++)
            {
                byte[] headerData = new byte[6];
                Array.Copy(data, index, headerData, 0, 6);
                this.Decode(headerData);
                Array.Copy(headerData, 0, data, index, 6);

                ushort opcode = BitConverter.ToUInt16(headerData, 0);
                int length = BitConverter.ToInt16(headerData, 2);

                Opcodes code = (Opcodes)opcode;

                Console.WriteLine("Got: " + code);

                byte[] packetData = new byte[length + 2];

                Array.Copy(data, index, packetData, 0, length + 2);

                index += 2 + (length - 1);

                switch ((Opcodes)opcode)
                {
                    case Opcodes.REQUEST_SCORES:
                        RequestScores(packetData);
                        break;
                    case Opcodes.SCORE_SUBMIT:
                        SubmitScore(packetData);
                        break;
                    default:
                        break;

                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error Reading Data.");
        }

    }  

    void SubmitScore(byte[] data)
    {
        PacketIn packet = new PacketIn(data);

        string user = packet.ReadString();
        uint score = packet.ReadUInt32();
        string song = packet.ReadString();
        string artist = packet.ReadString();

        Database.SQLResult result = DB._current.Select("SELECT * FROM `songscores` WHERE `Username` = '" + user + "' AND `SongName` = '" + RemoveSpecialCharacters(song) + "' AND `SongArtist` = '" + RemoveSpecialCharacters(artist) + "'");
        
        if (result.Count != 0)
        {
            uint currentScore = 0;
            for (int c = 0; c < result.Count; c++)
            {
                currentScore = (uint)result.Read<int>(c, "Score");
            }
            
            if(score > currentScore)
            {
                string sql = "UPDATE songscores SET Score = '" + score + "' WHERE `Username` = '" + user + "' AND `SongName` = '" + RemoveSpecialCharacters(song) + "' AND `SongArtist` = '" + RemoveSpecialCharacters(artist) + "'";
                using (var command = new MySqlCommand(sql, Database.Connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
        else
        {
            string sql = "INSERT INTO songscores (Username, SongName, Score, SongArtist) VALUES (@Username, @SongName, @Score, @SongArtist)";
            using (var command = new MySqlCommand(sql, Database.Connection))
            {
                command.Parameters.Add("@Username", MySqlDbType.VarChar).Value = user;
                command.Parameters.Add("@SongName", MySqlDbType.VarChar).Value = RemoveSpecialCharacters(song);
                command.Parameters.Add("@Score", MySqlDbType.Int32).Value = (int)score;
                command.Parameters.Add("@SongArtist", MySqlDbType.VarChar).Value = RemoveSpecialCharacters(artist);
                command.ExecuteNonQuery();
            }
        }
    }

    public static string RemoveSpecialCharacters(string str)
    {
        return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
    }

    void RequestScores(byte[] data)
    {
        PacketIn packet = new PacketIn(data);

        string UniqueID = packet.ReadString();
        string songName = packet.ReadString();
        string songArtist = packet.ReadString();

        Database.SQLResult result = DB._current.Select("SELECT * FROM `songscores` WHERE `SongName` = '" + RemoveSpecialCharacters(songName) + "' AND `SongArtist` = '" + RemoveSpecialCharacters(songArtist) + "'");

        PacketWriter SCORE_RESPONSE = new PacketWriter(Opcodes.SCORE_RESPONSE);

        SCORE_RESPONSE.WriteString(songName);

        if (result.Count != 0)
        {
            SCORE_RESPONSE.WriteUInt32((uint)result.Count);

            for (int c = 0; c < result.Count; c++)
            {
                SCORE_RESPONSE.WriteString(result.Read<string>(c, "Username"));
                SCORE_RESPONSE.WriteUInt32((uint)result.Read<int>(c, "Score"));
            }
        }
        else
        {
            SCORE_RESPONSE.WriteUInt32(0);
        }

        this.SendPacket(SCORE_RESPONSE);
    }    
    
    public void InitAuth()
    {
        while (true)
        {
            Thread.Sleep(1);
            if (AuthSocket.Available > 0)
            {
                DataBuffer = new byte[AuthSocket.Available];
                AuthSocket.Receive(DataBuffer, DataBuffer.Length, SocketFlags.None);

                HandleAuthData(DataBuffer);

            }
        }
    }

    public byte[] Encode(int size, int opcode)
    {
        var index = 0;
        var newSize = size + 2;
        var header = new byte[4];
        if (newSize > 0x7FFF)
        {
            header[index++] = (byte)(0x80 | (0xFF & (newSize >> 16)));
        }

        header[index++] = (byte)(0xFF & (newSize >> 8));
        header[index++] = (byte)(0xFF & newSize);
        header[index++] = (byte)(0xFF & opcode);
        header[index] = (byte)(0xFF & (opcode >> 8));

        return header;
    }

    public void Decode(byte[] header)
    {
        ushort length;
        short opcode;

        length = BitConverter.ToUInt16(new byte[] { header[1], header[0] }, 0);
        opcode = BitConverter.ToInt16(header, 2);

        header[0] = BitConverter.GetBytes(opcode)[0];
        header[1] = BitConverter.GetBytes(opcode)[1];

        header[2] = BitConverter.GetBytes(length)[0];
        header[3] = BitConverter.GetBytes(length)[1];
    }

    public void SendPacket(PacketWriter packet)
    {
        byte[] endData = FinalisePacket(packet);

        SendData(endData);
    }

    public byte[] FinalisePacket(PacketWriter packet)
    {

        BinaryWriter endPacket = new BinaryWriter(new MemoryStream());
        byte[] header = this.Encode(packet.PacketData.Length, (short)packet.Opcode);

        Console.WriteLine("Sent: " + packet.Opcode.ToString());

        endPacket.Write(header);
        endPacket.Write(packet.PacketData);

        var data = (endPacket.BaseStream as MemoryStream).ToArray();

        return data;
    }

    public void SendData(byte[] send)
    {

        var buffer = new byte[send.Length];
        Buffer.BlockCopy(send, 0, buffer, 0, send.Length);

        try
        {
            AuthSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, delegate { }, null);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error sending Packet.");
        }
    }
}