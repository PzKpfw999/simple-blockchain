using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Blockchain
{
    class Program
    {
        static void Main(string[] args)
        {
            Wallet alice = new Wallet();
            Wallet bob = new Wallet();
            Blockchain bc = new Blockchain();
            Help();
            while (true)
            {
                Console.Write("> ");
                string[] cmd = Console.ReadLine().Split(" ".ToCharArray());
                switch (cmd[0])
                {
                    case "AWI":
                        Console.WriteLine("PK: {0}", BitConverter.ToString(alice.PK).Replace("-", ""));
                        Console.WriteLine("Money: {0}", alice.GetMoney(bc));
                        break;
                    case "BWI":
                        Console.WriteLine("PK: {0}", BitConverter.ToString(bob.PK).Replace("-", ""));
                        Console.WriteLine("Money: {0}", bob.GetMoney(bc));
                        break;
                    case "AT":
                        if (!alice.Transfer(bc, alice.PK, bob.PK, Convert.ToInt32(cmd[1])))
                            Console.WriteLine("Fail to Commit the Transaction");
                        break;
                    case "BT":
                        if (!bob.Transfer(bc, bob.PK, alice.PK, Convert.ToInt32(cmd[1])))
                            Console.WriteLine("Fail to Commit the Transaction");
                        break;
                    case "AM":
                        alice.Mine(bc);
                        break;
                    case "BM":
                        bob.Mine(bc);
                        break;
                    case "BI":
                        if(cmd.Length==1)
                        {
                            Console.WriteLine("Blocks Count: {0}\n", bc.Blocks.Count);
                            foreach (Block b in bc.Blocks)
                            {
                                Console.WriteLine("Index: {0}", b.header.index);
                                Console.WriteLine("Timestamp: {0}", b.header.timestamp);
                                Console.WriteLine("Hash: {0}", BitConverter.ToString(b.hash).Replace("-", ""));
                                if (b.header.previous_hash != null)
                                    Console.WriteLine("Previous Hash: {0}",
                                        BitConverter.ToString(b.header.previous_hash).Replace("-", ""));
                                Console.WriteLine("Merkle_root: {0}\n", BitConverter.ToString(b.header.merkle_root).Replace("-", ""));
                            }
                        }
                        else
                        {
                            if(Convert.ToInt32(cmd[1])>=bc.Blocks.Count)
                            {
                                Console.WriteLine("Index Out of Range");
                                break;
                            }
                            Block b = bc.Blocks[Convert.ToInt32(cmd[1])];
                            Console.WriteLine("Index: {0}", b.header.index);
                            Console.WriteLine("Timestamp: {0}", b.header.timestamp);
                            Console.WriteLine("Hash: {0}", BitConverter.ToString(b.hash).Replace("-", ""));
                            if (b.header.previous_hash != null)
                                Console.WriteLine("Previous Hash: {0}",
                                    BitConverter.ToString(b.header.previous_hash).Replace("-", ""));
                            Console.WriteLine("Merkle_root: {0}\n", BitConverter.ToString(b.header.merkle_root).Replace("-", ""));
                            Console.WriteLine("-------------------------------------------------");
                            Console.WriteLine("Award Transaction: ");
                            Console.WriteLine("ID: {0}", BitConverter.ToString(b.awardTransaction.id).Replace("-", ""));
                            Console.WriteLine("Output: ");
                            Console.WriteLine("Receiver: {0}", BitConverter.ToString(b.awardTransaction.output.address).Replace("-", ""));
                            Console.WriteLine("Amount: {0}",b.awardTransaction.output.amount);
                            Console.WriteLine("-------------------------------------------------");
                            foreach (NormalTransaction _tx in b.transactions)
                            {
                                Console.WriteLine("Normal Transaction: ");
                                Console.WriteLine("ID: {0}\n", BitConverter.ToString(_tx.id).Replace("-", ""));
                                Console.WriteLine("Sig: {0}\n", BitConverter.ToString(_tx.signature).Replace("-", ""));
                                foreach (TxIn _txIn in _tx.inputs)
                                {
                                    Console.WriteLine("Input: ");
                                    Console.WriteLine("Out ID: {0}", BitConverter.ToString(_txIn.outId).Replace("-", ""));
                                    Console.WriteLine("Out Sub ID: {0}\n", _txIn.outIndex);
                                }
                                foreach(TxOut _txOut in _tx.outputs)
                                {
                                    Console.WriteLine("Output: ");
                                    Console.WriteLine("Receiver: {0}", BitConverter.ToString(_txOut.address).Replace("-", ""));
                                    Console.WriteLine("Amount: {0}\n", _txOut.amount);
                                }
                                Console.WriteLine("-------------------------------------------------");
                            }
                        }
                        break;
                    case "Q":
                        return; //quit the program
                    default:
                        Console.WriteLine("Unknown Input!");
                        break;
                }
            }
        }
        public static void Help()
        {
            Console.WriteLine("1. AWI                               //Alice Wallet Info");
            Console.WriteLine("2. BWI                               //Bob Wallet Info");
            Console.WriteLine("3. AT <Amount> (<Receiver's PK>)     //Alice Transfer");
            Console.WriteLine("4. BT <Amount> (<Receiver's PK>)     //Bob Transfer");
            Console.WriteLine("5. AM                                //Alice Mine");
            Console.WriteLine("6. BM                                //Bob Mine");
            Console.WriteLine("7. BI [index]                        //Blockchian Info");
            Console.WriteLine("8. Q                                 //Quit");
        }
        public static byte[] ConvertHexStringtoByte(string hex)
        {
            byte[] buffer = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length / 2; i++)
            {
                buffer[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return buffer;
        }
        public static bool CompareBytes(byte[] bt1, byte[] bt2)
        {
            if (bt1.Length != bt2.Length)
                return false;
            for (int i = 0; i < bt1.Length; i++)
                if (bt1[i] != bt2[i])
                    return false;
            return true;
        }
    }

    class Blockchain
    {
        private List<Block> blocks = new List<Block>();
        private List<NormalTransaction> transactions = new List<NormalTransaction>(); //transactions waiting for verify
        public List<Block> Blocks { get { return blocks; } }
        public List<NormalTransaction> Transactions { get { return transactions; } }
        public Blockchain()
        {
            Block genesisBlock = Block.CreateGenesisBlock();
            blocks.Add(genesisBlock);
        }
        public Block GetBlock(byte[] PK)
        {
            return new Block(blocks.Count, blocks[blocks.Count - 1].hash, 2, transactions.ToArray(), PK);
        }
        public bool CommitBlock(Block block)
        {
            if (block.IsValidBlock(blocks[blocks.Count - 1]))
            {
                foreach (NormalTransaction tx in block.transactions)
                {
                    if (!transactions.Contains(tx)) return false;
                    //if any transactions are not in the waiting pool, there must be someone commiting it first    
                }
                foreach (NormalTransaction tx in block.transactions)
                    transactions.Remove(tx);
                blocks.Add(block);
                return true;
            }
            else
                return false;
        }
        public bool CommitTransaction(NormalTransaction tx)
        {
            //make sure the id is exactly the hash of the transaction
            if (!Program.CompareBytes(tx.id, Crypto.CalculateSHA256(tx)))
            {
                Console.WriteLine("Transaction ID verification failed");
                return false;
            }
            bool found_flag;
            //make sure the TxIn is avaliable
            foreach (TxIn txIn in tx.inputs)
            {
                found_flag = false;
                for (int i = 1; i < blocks.Count; i++)
                {
                    //find out if the txIn point to an awardTransaction
                    if (txIn.outIndex == 0 && Crypto.VerifyHash(tx.id, tx.signature, blocks[i].awardTransaction.output.address))
                    {
                        found_flag = true;
                        break;
                    } 
                    foreach (NormalTransaction _tx in blocks[i].transactions)
                    {
                        if (Program.CompareBytes(_tx.id, txIn.outId))
                        {
                            if (_tx.outputs.Length > txIn.outIndex && Crypto.VerifyHash(tx.id, tx.signature, _tx.outputs[txIn.outIndex].address))
                            {
                                found_flag = true;
                            }
                            else
                                return false;
                        }
                        if (found_flag) break;
                    }
                    if (found_flag) break;
                }
                if (!found_flag) return false; //no found corresponding txOut of the txIn in blockchain
            }
            //provent double spend attack
            foreach (NormalTransaction _tx in transactions)
            {
                foreach (TxIn _txIn in _tx.inputs)
                {
                    foreach (TxIn txIn in tx.inputs)
                    {
                        if (_txIn == txIn) return false;
                    }
                }
            }
            transactions.Add(tx);
            return true;
        }
        public Dictionary<TxIn, int> FindUnspendTxOut(byte[] PK)
        {
            Dictionary<TxIn, int> txOuts = new Dictionary<TxIn, int>();
            for (int i = 1; i < blocks.Count; i++)
            {
                if (Program.CompareBytes(blocks[i].awardTransaction.output.address, PK))
                {
                    TxIn txIn = new TxIn();
                    txIn.outId = blocks[i].awardTransaction.id;
                    txIn.outIndex = 0;
                    txOuts.Add(txIn, blocks[i].awardTransaction.output.amount);
                }
                foreach (NormalTransaction tx in blocks[i].transactions)
                {
                    //find all transactions into the account
                    for (int j = 0; j < tx.outputs.Length; j++)
                    {
                        if (Program.CompareBytes(tx.outputs[j].address, PK))
                        {
                            TxIn txIn = new TxIn();
                            txIn.outId = tx.id;
                            txIn.outIndex = j;
                            txOuts.Add(txIn, tx.outputs[j].amount);
                        }
                    }
                    //subtract transactions already spent
                    foreach (TxIn txIn in tx.inputs)
                    {
                        if (txOuts.ContainsKey(txIn))
                        {
                            txOuts.Remove(txIn);
                        }
                    }
                }
            }
            foreach (NormalTransaction tx in transactions)
            {
                foreach(TxIn txIn in tx.inputs)
                {
                    if (txOuts.ContainsKey(txIn))
                    {
                        txOuts.Remove(txIn);
                    }
                }
            }
            return txOuts;
        }
    }
    class Wallet
    {
        CngKey key;
        public Wallet()
        {
            key = Crypto.GenerateECDSAKey();
        }
        public Wallet(CngKey key)
        {
            this.key = key;
        }
        public byte[] PK
        {
            get
            {
                return key.Export(CngKeyBlobFormat.EccPublicBlob);
            }
        }
        public bool Transfer(Blockchain bc, byte[] sender, byte[] receiver, int amount)
        {
            //calculate inputs
            List<TxIn> tx_ins = new List<TxIn>();
            int tx_in_amount_count = 0;
            foreach (KeyValuePair<TxIn, int> unspend_tx_out in bc.FindUnspendTxOut(sender))
            {
                if (tx_in_amount_count < amount)
                {
                    tx_in_amount_count += unspend_tx_out.Value;
                    tx_ins.Add(unspend_tx_out.Key);
                }
                else
                {
                    break;
                }
            }
            if (tx_in_amount_count < amount)
            {
                Console.WriteLine("Not enough money");
                return false;
                // not enough money in the account
            }

            TxOut[] outputs;
            //calculate ouputs
            if (tx_in_amount_count > amount)
            {
                outputs = new TxOut[2];
                outputs[1] = new TxOut();
                outputs[1].address = sender;
                outputs[1].amount = tx_in_amount_count - amount;
            }
            else
            {
                outputs = new TxOut[1];
            }
            outputs[0] = new TxOut();
            outputs[0].address = receiver;
            outputs[0].amount = amount;
            NormalTransaction tx = new NormalTransaction(tx_ins.ToArray(), outputs, key);
            return bc.CommitTransaction(tx);
        }
        public bool Mine(Blockchain bc)
        {
            Block b = bc.GetBlock(key.Export(CngKeyBlobFormat.EccPublicBlob));
            while (!b.IsDifficultyMatched())
                b.header.nonce++;
            Console.WriteLine("Difficulty: {0}", b.header.difficulty);
            Console.WriteLine("Nonce: {0}", b.header.nonce);
            Console.WriteLine("Hash: {0}", BitConverter.ToString(b.hash).Replace("-", ""));
            return bc.CommitBlock(b);
        }
        public int GetMoney(Blockchain bc)
        {
            var xx = bc.FindUnspendTxOut(key.Export(CngKeyBlobFormat.EccPublicBlob));
            int sum = 0;
            foreach (var x in xx)
            {
                sum += x.Value;
            }
            return sum;
        }
    }
}