using RotMG.Common;
using RotMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RotMG.Game
{
    public class Chunk
    {
        public HashSet<Entity> Entities;
        public readonly int X;
        public readonly int Y;

        public Chunk(int x, int y)
        {
            Entities = new HashSet<Entity>();
            X = x;
            Y = y;
        }

        public override int GetHashCode()
        {
            return (Y << 16) ^ X;
        }

        public override bool Equals(object obj)
        {
#if DEBUG
            if (obj is null || !(obj is Chunk))
                throw new Exception("Invalid object comparison.");
#endif
            return GetHashCode() == (obj as Chunk).GetHashCode();
        }
    }

    public class ChunkController
    {
        public const int Size = 8;
        public const int ActiveRadius = 32 / Size;

        public Chunk[,] Chunks;
        public int Width;
        public int Height;

        public ChunkController(int width, int height)
        {
            Width = width;
            Height = height;
            Chunks = new Chunk[Convert(Width) + 1, Convert(Height) + 1];
            for (int x = 0; x < Chunks.GetLength(0); x++)
                for (int y = 0; y < Chunks.GetLength(1); y++)
                    Chunks[x, y] = new Chunk(x, y);
        }

        public Chunk GetChunk(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Chunks.GetLength(0) || y >= Chunks.GetLength(1))
                return null;
            return Chunks[x, y];
        }

        public static int Convert(float value) => (int)Math.Ceiling(value / Size);

        public void Insert(Entity en)
        {
#if DEBUG
            if (en == null)
                throw new Exception("Entity is undefined.");
#endif
            int nx = Convert(en.Position.X);
            int ny = Convert(en.Position.Y);
            Chunk chunk = Chunks[nx, ny];

            if (en.CurrentChunk != chunk)
            {
                en.CurrentChunk?.Entities.Remove(en);
                en.CurrentChunk = chunk;
                en.CurrentChunk.Entities.Add(en);
            }
        }

        public void Remove(Entity en)
        {
#if DEBUG
            if (en == null)
                throw new Exception("Entity is undefined.");
            if (en.CurrentChunk == null)
                throw new Exception("Chunk is undefined.");
            if (!en.CurrentChunk.Entities.Contains(en))
                throw new Exception("Chunk doesn't contain entity.");
#endif
            en.CurrentChunk.Entities.Remove(en);
            en.CurrentChunk = null;
        }

        public List<Entity> HitTest(Position target, float radius)
        {
            List<Entity> result = new List<Entity>();
            int size = Convert(radius);
            int beginX = Convert(target.X);
            int beginY = Convert(target.Y);
            int startX = Math.Max(0, beginX - size);
            int startY = Math.Max(0, beginY - size);
            int endX = Math.Min(Chunks.GetLength(0) - 1, beginX + size);
            int endY = Math.Min(Chunks.GetLength(1) - 1, beginY + size);

            for (int x = startX; x <= endX; x++)
                for (int y = startY; y <= endY; y++)
                    foreach (Entity en in Chunks[x, y].Entities)
                        if (target.Distance(en) < radius)
                            result.Add(en);

            return result;
        }

        public List<Entity> GetActiveChunks(HashSet<Chunk> chunks)
        {
            List<Entity> result = new List<Entity>();
            foreach (Chunk c in chunks)
                foreach (Entity en in c.Entities)
                    result.Add(en);
            return result;
        }

        public void Dispose()
        {
            for (int w = 0; w < Chunks.GetLength(0); w++)
                for (int h = 0; h < Chunks.GetLength(1); h++)
                    Chunks[w, h].Entities.Clear();
            Chunks = null;
        }
    }
}
