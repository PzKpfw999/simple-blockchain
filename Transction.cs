using System.IO;
using System.Security.Cryptography;

namespace Blockchain
{
    class NormalTransaction : IHashable
    {
        public readonly byte[] id; //hash
        public readonly TxIn[] inputs;
        public readonly TxOut[] outputs;
        public readonly byte[] signature; //sign the id of this transaction
        public NormalTransaction(TxIn[] inputs,TxOut[] outputs,CngKey key)
        {
            this.inputs = inputs;
            this.outputs = outputs;
            id = Crypto.CalculateSHA256(this);
            signature = Crypto.SignHash(id,key);
        }
        public MemoryStream GetHashable()
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            foreach (TxIn _tx in inputs)
            {
                sw.Write(_tx.outId);
                sw.Write(_tx.outIndex);
            }
            foreach (TxOut _tx in outputs)
            {
                sw.Write(_tx.address);
                sw.Write(_tx.amount);
            }
            sw.Flush();
            return ms;
        }
    }
    class AwardTransaction : IHashable
    {
        const int MININGAWARD = 50; //mining award
        public readonly byte[] id; //hash
        public readonly int height; //block height to prevent the same transaction id 
        public readonly TxOut output; //only one output
        //PK is the miner's public key
        public AwardTransaction(byte[] PK,int height)
        {
            output = new TxOut();
            output.address = PK;
            output.amount = MININGAWARD;
            this.height = height;
            id = Crypto.CalculateSHA256(this);
        }
        public MemoryStream GetHashable()
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            if(output.address!=null)
                sw.Write(output.address);
            sw.Write(output.amount);
            sw.Write(height);
            sw.Flush();
            return ms;
        }
    }
    struct TxIn
    {
        public byte[] outId; //previous transaction id
        public int outIndex; //indicate which output in the previous transaction
        public static bool operator ==(TxIn txIn1, TxIn txIn2)
        {
            if (Program.CompareBytes(txIn1.outId,txIn2.outId) && txIn1.outIndex == txIn2.outIndex)
                return true;
            else 
                return false;
        }
        public static bool operator !=(TxIn txIn1, TxIn txIn2)
        {
            if (txIn1.outId != txIn2.outId || txIn1.outIndex != txIn2.outIndex)
                return true;
            else
                return false;
        }
    }
    struct TxOut
    {
        public byte[] address; //receiver's PK
        public int amount;
    }
}
