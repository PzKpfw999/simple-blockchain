using System.Collections.Generic;
using System.IO;

namespace Blockchain
{
    struct BlockHeader
    {
        public int index;
        public long timestamp;
        public byte[] previous_hash;
        public byte[] merkle_root;
        public byte difficulty; //indicate how many 0 at the top of hash
        public int nonce;
    }
    class Block : IHashable
    {
        public BlockHeader header;
        public AwardTransaction awardTransaction;
        public NormalTransaction[] transactions;
        public MerkleTree tree;
        public byte[] hash;
        public Block(int index, byte[] previous_hash, byte difficulty, NormalTransaction[] transactions, byte[] PK)
        {
            header.index = index;
            header.timestamp = Crypto.GetTimestamp();
            header.previous_hash = previous_hash;
            header.difficulty = difficulty;
            header.nonce = 0;
            awardTransaction = new AwardTransaction(PK, index);
            this.transactions = transactions;
            tree = new MerkleTree(this);
            header.merkle_root = tree.Root;
            hash = null;
        }
        public static Block CreateGenesisBlock()
        {
            Block genesisBlock = new Block(0, null, 0, new NormalTransaction[0], null);
            genesisBlock.hash = Crypto.GetRandomBytes(32);
            return genesisBlock;
        }
        public bool IsDifficultyMatched()
        {
            hash = Crypto.CalculateSHA256(this);
            for (int i = 0; i < header.difficulty; i++)
            {
                if (hash[i] != 0)
                    return false;
            }
            return true;
        }
        public bool IsValidBlock(Block previous_block)
        {
            if (header.index == previous_block.header.index + 1 &&
                Program.CompareBytes(header.previous_hash, previous_block.hash) &&
                IsDifficultyMatched())
                return true;
            else
                return false;
        }
        public MemoryStream GetHashable()
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.AutoFlush = true;
            sw.Write(header.index);
            sw.Write(header.timestamp);
            if (header.previous_hash != null)
                sw.Write(header.previous_hash);
            sw.Write(header.merkle_root);
            sw.Write(header.difficulty);
            sw.Write(header.nonce);
            return ms;
        }
    }
    class MerkleTree
    {
        private byte[][] tree;
        private int leavesNumber;
        public byte[] Root { get { return tree[tree.Length - 1]; } }
        public MerkleTree(Block block)
        {
            List<byte[]> temp = new List<byte[]>(block.transactions.Length + 1);
            temp.Add(block.awardTransaction.id);
            foreach (NormalTransaction tx in block.transactions)
            {
                temp.Add(tx.id);
            }
            Build(temp.ToArray());
        }
        private void Build(byte[][] leaves)
        {
            List<byte[]> temp = new List<byte[]>();
            int layerLength = 0;
            int offset = 0;
            byte[] buffer = new byte[64]; //allocate space for concatnate hash
            for (int i = 0; i < leaves.Length; i++)
            {
                temp.Add(leaves[i]);
                layerLength++;
            }
            leavesNumber = layerLength; // the bottom layer nodes number
            while (layerLength != 1)
            {
                if (layerLength % 2 != 0)
                {
                    for (int i = offset; i < layerLength - 1; i += 2)
                    {
                        temp[i].CopyTo(buffer, 0);
                        temp[i + 1].CopyTo(buffer, 32);
                        temp.Add(Crypto.CalculateSHA256(buffer));
                    }
                    temp[temp.Count - 1].CopyTo(buffer, 0);
                    temp[temp.Count - 1].CopyTo(buffer, 32);
                    temp.Add(Crypto.CalculateSHA256(buffer));
                    offset += layerLength;
                    layerLength = layerLength / 2 + 1;
                }
                else
                {
                    for (int i = offset; i < layerLength; i += 2)
                    {
                        temp[i].CopyTo(buffer, 0);
                        temp[i + 1].CopyTo(buffer, 32);
                        temp.Add(Crypto.CalculateSHA256(buffer));
                    }
                    offset += layerLength;
                    layerLength /= 2;
                }
            }
            tree = temp.ToArray();
        }
        public bool Verify(NormalTransaction tx)
        {
            /*
            bool flag = false;
            for(int i = 0;i<leavesNumber;i++)
            {
                if (tx.id == tree[i])
                {
                    flag = true;
                    break;
                }
            }
            if (flag == false) return false;
            */
            return true;
        }
    }
}
