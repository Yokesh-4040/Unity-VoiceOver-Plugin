using System;
using System.IO;
using UnityEngine;

namespace FF.ElevenLabs.Editor
{
    public static class SavWav
    {
        public static bool Save(string filepath, AudioClip clip)
        {
            if (!filepath.ToLower().EndsWith(".wav"))
                filepath += ".wav";

            var parent = Directory.GetParent(filepath);
            if (!parent.Exists) parent.Create();

            using (var fileStream = CreateEmpty(filepath))
            {
                ConvertAndWrite(fileStream, clip);
                WriteHeader(fileStream, clip);
            }

            return true; 
        }

        static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < 44; i++) // Assume 44 bytes header
                fileStream.WriteByte(emptyByte);

            return fileStream;
        }

        static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            Int16[] intData = new Int16[samples.Length];
            Byte[] bytesData = new Byte[samples.Length * 2];
            int rescaleFactor = 32767;

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                Byte[] byteArr = new Byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        static void WriteHeader(FileStream fileStream, AudioClip clip)
        {
            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            Byte[] audioFormat = BitConverter.GetBytes(1); // PCM
            fileStream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            Byte[] sampleRate = BitConverter.GetBytes(hz);
            fileStream.Write(sampleRate, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
            fileStream.Write(byteRate, 0, 4);

            Byte[] blockAlign = BitConverter.GetBytes(channels * 2);
            fileStream.Write(blockAlign, 0, 2);

            Byte[] bitsPerSample = BitConverter.GetBytes(16);
            fileStream.Write(bitsPerSample, 0, 2);

            Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(dataString, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
        }
    }
}
