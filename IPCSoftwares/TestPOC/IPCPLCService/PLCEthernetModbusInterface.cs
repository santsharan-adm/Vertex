using CommonLibrary.Interfaces;
using CommonLibrary.Models;
using IPCPLCService.Events;
using ModbusLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPCPLCService
{
    public class PLCEthernetModbusInterface: PLCInterface
    {
        readonly uint PLC_BLOCK = 20;
        

        


        public PLCEthernetModbusInterface()
        {
        }
        public override void OnStart(object? item)
        {
            base.OnStart(item);
            CreateBlocks();
        }
        //public void CreateBlocks()
        //{
        //    foreach (KeyValuePair<PLCTagModel,PLCData> plcData in PLC.Data)
        //    {
        //        //uint modbusOffsetAddress = GetPLCModbusOffsetAddress( plcData.Key.ModbusAddress);
        //        uint blockIndex = plcData.Key.ModbusAddress / PLC_BLOCK;
        //        //PLCBlock block = PLC.Blocks[blockIndex];
        //        if (!PLC.Blocks.ContainsKey(blockIndex))
        //        {
        //            lock (obLockPlcBlock)
        //            {
        //                PLCBlock block = new PLCBlock() { Id = blockIndex };
        //                PLC.Blocks.Add(blockIndex, block);
        //            }
        //        }
        //    }
        //}


        public void CreateBlocks()
        {
            foreach (KeyValuePair<PLCTagModel, PLCData> plcData in PLC.Data)
            {
                uint blockIndex = plcData.Key.ModbusAddress / PLC_BLOCK;

                // Ensure block exists
                if (!PLC.Blocks.ContainsKey(blockIndex))
                {
                    lock (obLockPlcBlock)
                    {
                        PLCBlock block = new PLCBlock() { Id = blockIndex };

                        // Initialize block data array (size = PLC_BLOCK registers)
                       // block.Data = new byte[PLC_BLOCK * 2]; // Each register = 2 bytes
                        PLC.Blocks.Add(blockIndex, block);
                    }
                }

                // Handle FLOAT and STRING allocation logic
                switch (plcData.Key.AlgoNo)
                {
                    case 1:
                        // FLOAT = 4 bytes (2 registers)
                        EnsureNextBlockIfNeeded(plcData.Key.ModbusAddress, 2);
                        break;

                    case 2:
                        // STRING length in bytes (each register = 2 bytes)
                        int registersNeeded = (int)Math.Ceiling(plcData.Key.DataLength / 2.0);
                        EnsureNextBlockIfNeeded(plcData.Key.ModbusAddress, registersNeeded);
                        break;

                    default:
                        // INT or SHORT fits in one register, no extra handling needed
                        break;
                }
            }
        }

        // ✅ Helper: Ensure next block exists if data spans multiple blocks
        private void EnsureNextBlockIfNeeded(uint startAddress, int registersNeeded)
        {
            uint startBlockIndex = startAddress / PLC_BLOCK;
            uint offsetInBlock = startAddress % PLC_BLOCK;

            int spaceInCurrentBlock = (int)(PLC_BLOCK - offsetInBlock);
            if (registersNeeded > spaceInCurrentBlock)
            {
                uint nextBlockIndex = startBlockIndex + 1;
                if (!PLC.Blocks.ContainsKey(nextBlockIndex))
                {
                    lock (obLockPlcBlock)
                    {
                        PLCBlock nextBlock = new PLCBlock() { Id = nextBlockIndex };
                        //nextBlock.Data = new byte[PLC_BLOCK * 2];
                        PLC.Blocks.Add(nextBlockIndex, nextBlock);
                    }
                }
            }
        }


        //protected override void LoadConfiguration(PLCModel plc)
        //{
        //    base.LoadConfiguration(plc);
        //    Dictionary<int, string[]> items= _plcTagConfigurationHandle.ReadFile();
        //    foreach (var item in items)
        //    {
        //        PLCTagModel tag = new PLCTagModel();
        //        tag.LoadFromStringArray(item.Value);
        //        if (tag.PLCNo == plc.PLCNo)
        //        {
        //            PLCData data = new PLCData(plc.PLCNo,tag.ModbusAddress,0);
        //            plc.Data.Add(tag, data);
        //        }
        //    }

        //}

        protected override void StartPolling()
        {

            NetworkStream stream = client.GetStream();
            EthernetModbus modbus = new EthernetModbus((byte)PLC.PLCNo, stream);

            while (!ShudownInitiated)
            {
                try
                {
                    ///Write Operations
                    lock (obLockPlcWriteList)
                    {
                        Dictionary<PLCTagModel, object> tempList = new Dictionary<PLCTagModel, object>(plcWriteList);
                        foreach (KeyValuePair<PLCTagModel, object> kvp in plcWriteList)
                        {
                            uint modbusStartAddress = kvp.Key.ModbusAddress;
                            uint functionCode = GetPLCModbusFunctionCode(modbusStartAddress, false);
                            ushort modbusOffsetAddress = (ushort)GetPLCModbusOffsetAddress(modbusStartAddress);
                            bool[] responseDataBits = null;
                            PLCData? plcData = null;
                            if (!PLC.Data.TryGetValue(kvp.Key, out plcData))
                            {
                                plcData = new PLCData(PLC.PLCNo, kvp.Key.ModbusAddress, 0, kvp.Key.AlgoNo);
                                PLC.Data.Add(kvp.Key, plcData);
                            }
                            if (functionCode == 5)
                            {
                                responseDataBits = modbus.WriteSingleCoil(modbusOffsetAddress, plcData.Value);
                            }
                            else if (functionCode == 6)
                            {
                                responseDataBits = modbus.WriteSingleRegister(modbusOffsetAddress, plcData.Value);
                            }
                            else
                            {
                                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface",
                                    $"Invalid function code for read operation - {functionCode}, ModbusAddress is{modbusStartAddress}", ""));
                            }

                            ProcessResponse(responseDataBits, kvp.Key);
                            tempList.Add(kvp.Key, kvp.Value);

                        }
                        if (tempList.Count > 0)
                        {
                            foreach (KeyValuePair<PLCTagModel, object> plcTag in tempList)
                            {
                                plcWriteList.Remove(plcTag.Key);
                            }
                        }
                    }
                    ///Read Operations
                    lock (obLockPlcBlock)
                    {
                        foreach (var block in PLC.Blocks)
                        {
                            uint modbusStartAddress = block.Key * PLC_BLOCK;
                            uint functionCode = GetPLCModbusFunctionCode(modbusStartAddress, true);
                            ushort modbusOffsetAddress = (ushort)GetPLCModbusOffsetAddress(modbusStartAddress);
                            // Send a request or keep-alive message
                            ushort[] responseData = null;
                            bool[] responseDataBits = null;
                            if (functionCode == 1)
                            {
                                responseDataBits = modbus.ReadCoils(modbusOffsetAddress, (ushort)PLC_BLOCK);
                            }
                            else if (functionCode == 2)
                            {
                                responseDataBits = modbus.ReadDiscreteInputs(modbusOffsetAddress, (ushort)PLC_BLOCK);
                            }
                            else if (functionCode == 3)
                            {
                                responseData = modbus.ReadHoldingRegisters(modbusOffsetAddress, (ushort)PLC_BLOCK);
                            }
                            else if (functionCode == 4)
                            {
                                responseData = modbus.ReadInputRegisters(modbusOffsetAddress, (ushort)PLC_BLOCK);
                            }
                            else
                            {
                                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface",
                                    $"Invalid function code for read operation - {functionCode}, ModbusAddress is{modbusStartAddress}", ""));
                            }




                            //string hex = BitConverter.ToString(request).Replace("-", " ");

                            //RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface",
                            //    $"Sending Request. {hex}", ""));


                            if (functionCode == 1 || functionCode == 2)
                            {
                                // Process bit response if needed
                                continue;
                            }
                            else if (functionCode == 3 || functionCode == 4)
                            {
                                if (responseData != null)
                                {
                                    //string response = Encoding.UTF8.GetString((byte[])responseData, 0, responseData.Length);

                                    ///Console.WriteLine("Received: " + response);

                                    //string hex = BitConverter.ToString(responseData).Replace("-", " ");

                                    //RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface", $"Response Received. {hex}", ""));
                                    //ProcessResponse(buffer, modbusAddress, PLCBlock);
                                    ProcessResponse(responseData, block.Value);
                                }
                                else
                                {
                                    RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface",
                                    $"Error in response data from PLC. (NULL) received", ""));
                                }
                            }
                        }

                        ProcessPLCBlocks();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    string err = ErrorModel.GetErrorExceptionDetail(ex);
                    RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                           $"Error occured during polling. Plc-{PLC.PLCNo}, Name-{PLC.Name}. Error is {err}", ""));
                }
                // Wait before polling again
                Thread.Sleep(1000); // 1 second delay
            }
        }
        //void ProcessPLCBlocks()
        //{
        //    foreach (KeyValuePair<PLCTagModel, PLCData> plcData in PLC.Data)
        //    {
        //        uint modbusOffsetAddress = GetPLCModbusOffsetAddress(plcData.Key.ModbusAddress);
        //        uint blockIndex = plcData.Key.ModbusAddress / PLC_BLOCK;
        //        uint offsetInBlock = plcData.Key.ModbusAddress % PLC_BLOCK;
        //        if (PLC.Blocks.ContainsKey(blockIndex))
        //        {
        //            PLCBlock block = PLC.Blocks[blockIndex];
        //            if (offsetInBlock < block.Data.Length)
        //            {
        //                plcData.Value.Value = block.Data[offsetInBlock];
        //                OnRaiseModbusResponseReceived(plcData.Key.Id, plcData.Value);
        //            }
        //            else
        //            {
        //                RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
        //               $"Offset in block exceeds block data length. Plc-{PLC.PLCNo}, Name-{PLC.Name}, Block-{blockIndex}, Offset-{offsetInBlock}", ""));
        //            }
        //        }
        //        else
        //        {
        //            RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
        //               $"Block not found in PLC blocks. Plc-{PLC.PLCNo}, Name-{PLC.Name}, Block-{blockIndex}", ""));
        //        }
        //    }
        //}


        //void ProcessPLCBlocks()
        //{
        //    foreach (KeyValuePair<PLCTagModel, PLCData> plcData in PLC.Data)
        //    {
        //        uint modbusAddress = plcData.Key.ModbusAddress;
        //        uint blockIndex = modbusAddress / PLC_BLOCK;
        //        uint offsetInBlock = modbusAddress % PLC_BLOCK;

        //        // Check if block exists
        //        if (!PLC.Blocks.ContainsKey(blockIndex))
        //        {
        //            RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
        //                $"Block not found. Plc-{PLC.PLCNo}, Name-{PLC.Name}, Block-{blockIndex}", ""));
        //            continue;
        //        }

        //        PLCBlock block = PLC.Blocks[blockIndex];
        //       if( plcData.Key.AlgoNo ==0)
        //        {
        //            // Normal integer or short
        //            if (offsetInBlock < block.Data.Length)
        //            {
        //                plcData.Value.Value = block.Data[offsetInBlock];
        //            }
        //            else
        //            {
        //                RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
        //                    $"Offset exceeds block length. Plc-{PLC.PLCNo}, Name-{PLC.Name}, Block-{blockIndex}, Offset-{offsetInBlock}", ""));
        //            }
        //        }
        //        // Handle FLOAT (4 bytes = 2 Modbus registers)
        //        else if (plcData.Key.AlgoNo == 1)
        //        {
        //            byte[] floatBytes = new byte[4];

        //            // Each register = 2 bytes, so float spans 2 registers
        //            int bytesNeeded = 4;
        //            int bytesAvailable =(int)( (block.Data.Length - offsetInBlock) * 2);

        //            if (bytesAvailable >= bytesNeeded)
        //            {
        //                // All bytes are in the same block
        //                Buffer.BlockCopy(block.Data, (int)(offsetInBlock * 2), floatBytes, 0, 4);
        //            }
        //            else
        //            {
        //                // Float spans two blocks
        //                int firstPart = bytesAvailable;
        //                Buffer.BlockCopy(block.Data,(int)( offsetInBlock * 2), floatBytes, 0, firstPart);

        //                // Get next block
        //                uint nextBlockIndex = blockIndex + 1;
        //                if (PLC.Blocks.ContainsKey(nextBlockIndex))
        //                {
        //                    PLCBlock nextBlock = PLC.Blocks[nextBlockIndex];
        //                    Buffer.BlockCopy(nextBlock.Data, 0, floatBytes, firstPart, 4 - firstPart);
        //                }
        //                else
        //                {
        //                    RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
        //                        $"Next block not found for float spanning blocks. Plc-{PLC.PLCNo}, Name-{PLC.Name}", ""));
        //                    continue;
        //                }
        //            }

        //            // Convert bytes to float (handle endianness if needed)
        //            if (plcData.Key.IsBigEndian)
        //                Array.Reverse(floatBytes);

        //            plcData.Value.Value = BitConverter.ToSingle(floatBytes, 0);
        //        }
                

        //        OnRaiseModbusResponseReceived(plcData.Key.Id, plcData.Value);
        //    }
        //}


        void ProcessPLCBlocks()
        {
            foreach (KeyValuePair<PLCTagModel, PLCData> plcData in PLC.Data)
            {
                uint modbusAddress = plcData.Key.ModbusAddress;
                uint blockIndex = modbusAddress / PLC_BLOCK;
                uint offsetInBlock = modbusAddress % PLC_BLOCK;

                if (!PLC.Blocks.ContainsKey(blockIndex))
                {
                    RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                        $"Block not found. Plc-{PLC.PLCNo}, Name-{PLC.Name}, Block-{blockIndex}", ""));
                    continue;
                }

                PLCBlock block = PLC.Blocks[blockIndex];

                // Handle different data types
                switch (plcData.Key.AlgoNo)
                {
                    //case 1: // DINT (32-bit signed integer)
                    //    plcData.Value.Value = ReadDIntFromBlocks(blockIndex, offsetInBlock, plcData.Key.IsBigEndian);
                    //    break;
                    case 0:
                        // INT or SHORT
                        if (plcData.Key.DataLength == 1)
                        {
                            if (offsetInBlock < block.Data.Length)
                            {
                                plcData.Value.Value = block.Data[offsetInBlock];
                            }
                            else
                            {
                                RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                                    $"Offset exceeds block length. Plc-{PLC.PLCNo}, Name-{PLC.Name}, Block-{blockIndex}, Offset-{offsetInBlock}", ""));
                            }
                        }
                        else if (plcData.Key.DataLength == 2)
                        {

                            // DINT (32-bit signed integer)
                            plcData.Value.Value = ReadDIntFromBlocks(blockIndex, offsetInBlock, plcData.Key.IsBigEndian);
                        }
                        else
                        {

                            RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                                $"Invalid Data length specified for linear conversion. Plc-{PLC.PLCNo}, Name-{PLC.Name}, Block-{blockIndex}, Offset-{offsetInBlock}", ""));
                        }
                        break;

                    case 1: //// Float (32-bit signed integer)
                        plcData.Value.Value = ReadFloatFromBlocks(blockIndex, offsetInBlock, plcData.Key.IsBigEndian);
                        break;

                    case 2:
                        plcData.Value.Value = ReadStringFromBlocks(blockIndex, offsetInBlock, plcData.Key.DataLength);
                        break;


                    case 3: // BOOLEAN (single bit in a word)
                        plcData.Value.Value = ReadBooleanFromBlocks(blockIndex, offsetInBlock, plcData.Key.BitIndex);
                        break;



                    default:
                        RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                                $"Invalid algorithm. Plc-{PLC.PLCNo}, Name-{PLC.Name}, Block-{blockIndex}, Offset-{offsetInBlock}", ""));
                        break;
                }

                OnRaiseModbusResponseReceived(plcData.Key.Id, plcData.Value);
            }
        }

        //// Helper: Read Float (4 bytes = 2 registers)
        //private float ReadFloatFromBlocks(uint blockIndex, uint offsetInBlock, bool isBigEndian)
        //{
        //    byte[] floatBytes = new byte[4];
        //    PLCBlock block = PLC.Blocks[blockIndex];

        //    int bytesNeeded = 4;
        //    int bytesAvailable =(int)( (block.Data.Length - offsetInBlock) * 2);

        //    if (bytesAvailable >= bytesNeeded)
        //    {
        //        Buffer.BlockCopy(block.Data, (int)offsetInBlock * 2, floatBytes, 0, 4);
        //    }
        //    else
        //    {
        //        int firstPart = bytesAvailable;
        //        Buffer.BlockCopy(block.Data, (int)offsetInBlock * 2, floatBytes, 0, firstPart);

        //        uint nextBlockIndex = blockIndex + 1;
        //        if (PLC.Blocks.ContainsKey(nextBlockIndex))
        //        {
        //            PLCBlock nextBlock = PLC.Blocks[nextBlockIndex];
        //            Buffer.BlockCopy(nextBlock.Data, 0, floatBytes, firstPart, 4 - firstPart);
        //        }
        //        else
        //        {
        //            throw new Exception("Next block not found for float spanning blocks.");
        //        }
        //    }

        //    if (isBigEndian) Array.Reverse(floatBytes);
        //    return BitConverter.ToSingle(floatBytes, 0);
        //}

        // Helper: Read FLOAT (4 bytes = 2 registers)
        private float ReadFloatFromBlocks(uint blockIndex, uint offsetInBlock, bool isBigEndian)
        {
            byte[] floatBytes = ReadBytesAcrossBlocks(blockIndex, offsetInBlock, 4);
            if (isBigEndian) Array.Reverse(floatBytes);
            return BitConverter.ToSingle(floatBytes, 0);
        }


        //  Helper: Read DINT (4 bytes = 2 registers)
        private int ReadDIntFromBlocks(uint blockIndex, uint offsetInBlock, bool isBigEndian)
        {
            byte[] dintBytes = ReadBytesAcrossBlocks(blockIndex, offsetInBlock, 4);
            if (isBigEndian) Array.Reverse(dintBytes);
            return BitConverter.ToInt32(dintBytes, 0);
        }

        // Helper: Read BOOLEAN (single bit in a word)
        private bool ReadBooleanFromBlocks(uint blockIndex, uint offsetInBlock, int bitIndex)
        {
            PLCBlock block = PLC.Blocks[blockIndex];

            if (offsetInBlock >= block.Data.Length)
                throw new Exception($"Offset exceeds block length for Boolean read.");

            uint wordValue = block.Data[offsetInBlock]; // Each register is 16-bit
            return (wordValue & (1 << bitIndex)) != 0;
        }



        // ✅ Helper: Read String (ASCII, may span multiple blocks)
        private string ReadStringFromBlocks(uint blockIndex, uint offsetInBlock, int length)
        {
            List<byte> stringBytes = new List<byte>();
            int bytesNeeded = length;
            uint currentBlockIndex = blockIndex;
            uint currentOffset = offsetInBlock;

            while (bytesNeeded > 0 && PLC.Blocks.ContainsKey(currentBlockIndex))
            {
                PLCBlock block = PLC.Blocks[currentBlockIndex];
                int bytesAvailable =(int)( (block.Data.Length - currentOffset) * 2);
                int toCopy = Math.Min(bytesAvailable, bytesNeeded);

                byte[] temp = new byte[toCopy];
                Buffer.BlockCopy(block.Data, (int)currentOffset * 2, temp, 0, toCopy);
                stringBytes.AddRange(temp);

                bytesNeeded -= toCopy;
                currentBlockIndex++;
                currentOffset = 0; // Next block starts at 0
            }

            return System.Text.Encoding.ASCII.GetString(stringBytes.ToArray()).TrimEnd('\0');
        }


        // ✅ Generic Helper: Read N bytes across blocks
        private byte[] ReadBytesAcrossBlocks(uint blockIndex, uint offsetInBlock, int bytesNeeded)
        {
            byte[] result = new byte[bytesNeeded];
            PLCBlock block = PLC.Blocks[blockIndex];

            int bytesAvailable = (int)((block.Data.Length - offsetInBlock) * 2);

            if (bytesAvailable >= bytesNeeded)
            {
                Buffer.BlockCopy(block.Data, (int)offsetInBlock * 2, result, 0, bytesNeeded);
            }
            else
            {
                int firstPart = bytesAvailable;
                Buffer.BlockCopy(block.Data, (int)offsetInBlock * 2, result, 0, firstPart);

                uint nextBlockIndex = blockIndex + 1;
                if (PLC.Blocks.ContainsKey(nextBlockIndex))
                {
                    PLCBlock nextBlock = PLC.Blocks[nextBlockIndex];
                    Buffer.BlockCopy(nextBlock.Data, 0, result, firstPart, bytesNeeded - firstPart);
                }
                else
                {
                    throw new Exception($"Next block not found for data spanning blocks.");
                }
            }

            return result;
        }


        protected override void ProcessResponse(byte[] response, uint startaddress, ushort noOfRegisters)
        {
            if (response.Length < 9)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                       $"Invalid response length. {response.Length} bytes received.", ""));
                return;
            }

            // Extract header fields
            ushort transactionId = (ushort)((response[0] << 8) | response[1]);
            ushort protocolId = (ushort)((response[2] << 8) | response[3]);
            ushort length = (ushort)((response[4] << 8) | response[5]);
            byte unitId = response[6];
            byte functionCode = response[7];
            byte byteCount = response[8];

            RaiseError(new ErrorModel(0, Severity.Information, "PLC Interface",
                       $"Response received. Transaction ID: {transactionId}, Protocol ID: {protocolId}, Unit ID: {unitId}, Function Code: {functionCode}, Byte Count: {byteCount}", ""));

            if ((noOfRegisters * 2) != byteCount)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                       $"Response data mismatch. Expected byte count {(noOfRegisters * 2)}, Received {byteCount}", ""));

            }

            // Extract register values
            for (int i = 0; i < byteCount / 2; i++)
            {
                int index = 9 + i * 2;
                ushort registerValue = (ushort)((response[index] << 8) | response[index + 1]);
                //OnRaiseModbusResponseReceived()
                //if (RaiseModbusResponseReceivedEvent != null)
                //{
                //    RaiseModbusResponseReceivedEvent(this, unitId, (uint)(startaddress + i), registerValue);
                //}

            }
        }

        void ProcessResponse(byte[] response, PLCBlock block)
        {
            uint modbusAddress = block.Id * PLC_BLOCK;
            if (response.Length < 9)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                       $"Invalid response length. {response.Length} bytes received.", ""));
                return;
            }

            // Extract header fields
            ushort transactionId = (ushort)((response[0] << 8) | response[1]);
            ushort protocolId = (ushort)((response[2] << 8) | response[3]);
            ushort length = (ushort)((response[4] << 8) | response[5]);
            byte unitId = response[6];
            byte functionCode = response[7];
            byte byteCount = response[8];

            RaiseError(new ErrorModel(0, Severity.Information, "PLC Interface",
                       $"Response received. Transaction ID: {transactionId}, Protocol ID: {protocolId}, Unit ID: {unitId}, Function Code: {functionCode}, Byte Count: {byteCount}", ""));

            if ((PLC_BLOCK * 2) != byteCount)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                       $"Response data mismatch. Expected byte count {(PLC_BLOCK * 2)}, Received {byteCount}", ""));

            }

            // Extract register values
            for (int i = 0; i < byteCount / 2; i++)
            {
                int index = 9 + i * 2;
                ushort registerValue = (ushort)((response[index] << 8) | response[index + 1]);
                block.Data[i] = registerValue;

                //if (RaiseModbusResponseReceivedEvent != null)
                //{
                //    RaiseModbusResponseReceivedEvent(this, unitId, (uint)(modbusAddress + i), registerValue);
                //}

            }
        }

        void ProcessResponse(ushort[] response, PLCBlock block)
        {
            int length=response.Length;

            for (int i = 0; i < length; i++)
            {               
                block.Data[i] = response[i];
            }
        }
        void ProcessResponse(bool[] response, PLCTagModel tag)
        {
            int length = response.Length;
            if (length > 0)
            {
                if (PLC.Data.TryGetValue(tag, out PLCData? plcData))
                {
                    plcData.Value = response[0];
                }
            }
        }

        uint GetPLCModbusOffsetAddress(uint modbusAddress)
        {

            if (modbusAddress < 10)
                return 0;

            uint divisor = 1;
            while (modbusAddress / divisor >= 10)
            {
                divisor *= 10;
            }

            return modbusAddress % divisor;

        }

        uint GetPLCModbusFunctionCode(uint modbusAddress, bool isRead, bool isMultipleWriteOperation=false)
        {
            if (isRead)
            {
                if (modbusAddress >= 1 && modbusAddress <= 9999)
                {
                    return 1; // Read Coils
                }
                else if (modbusAddress >= 10001 && modbusAddress <= 19999)
                {
                    return 2; // Read Discrete Inputs
                }
                else if (modbusAddress >= 30001 && modbusAddress <= 39999)
                {
                    return 4; // Read Input Registers
                }
                else if (modbusAddress >= 40000 && modbusAddress <= 49999)
                {
                    return 3; // Read Holding Registers
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(modbusAddress), "Invalid Modbus address range for read operation.");
                }
            }
            else
            {
                if (modbusAddress >= 1 && modbusAddress <= 9999)
                {
                    return 5; // Write Single Coil
                }
                else if (modbusAddress >= 40001 && modbusAddress <= 49999)
                {
                    if(!isMultipleWriteOperation)
                        return 6; // Write Single Register
                    else
                        return 16; // Write Multiple Registers                   
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(modbusAddress), "Invalid Modbus address range for write operation.");
                }
            }


        }

       
       


       

    }
}
