using System.Net.Sockets;

namespace ModbusLibrary
{
    public class EthernetModbus
    {
        byte _slaveId;
        NetworkStream _stream;

        public EthernetModbus(byte slaveId, NetworkStream stream)
        {
            this._slaveId = slaveId;
            _stream = stream;
        }

        ushort[] ProcessResponse(byte[] response, ushort numRegisters)
        {
            if (response.Length < 9)
            {
                throw new Exception($"Invalid response length. {response.Length} bytes received.");
            }

            // Extract header fields
            ushort transactionId = (ushort)((response[0] << 8) | response[1]);
            ushort protocolId = (ushort)((response[2] << 8) | response[3]);
            ushort length = (ushort)((response[4] << 8) | response[5]);
            byte unitId = response[6];
            byte functionCode = response[7];
            byte byteCount = response[8];

            // Check for Modbus exception response (function code has MSB set)
            CheckForModbusException(response);

            if ((numRegisters * 2) != byteCount)
            {
                throw new Exception($"Response data mismatch. Expected byte count {(numRegisters * 2)}, Received {byteCount}");
            }
            ushort[] values = new ushort[numRegisters];
            // Extract register values
            for (int i = 0; i < byteCount / 2; i++)
            {
                int index = 9 + i * 2;
                values[i] = (ushort)((response[index] << 8) | response[index + 1]);
            }
            return values;
        }

        bool[] ProcessResponseDiscrete(byte[] response, ushort discrreteIO)
        {
            if (response.Length < 9)
            {
                throw new Exception($"Invalid response length. {response.Length} bytes received.");
            }

            // Extract header fields
            ushort transactionId = (ushort)((response[0] << 8) | response[1]);
            ushort protocolId = (ushort)((response[2] << 8) | response[3]);
            ushort length = (ushort)((response[4] << 8) | response[5]);
            byte unitId = response[6];
            byte functionCode = response[7];
            byte byteCount = response[8];
            ushort numRegisters = (ushort)((discrreteIO + 7) / 8);

            // Check for Modbus exception response (function code has MSB set)
            CheckForModbusException(response);

            if ((numRegisters != byteCount))
            {
                throw new Exception($"Response data mismatch. Expected byte count {(numRegisters * 2)}, Received {byteCount}");
            }
            bool[] discrreteIOs = new bool[discrreteIO];
            for (int i = 0; i < discrreteIO; i++)
            {
                int byteIndex = 9 + (i / 8);
                int bitIndex = i % 8;
                discrreteIOs[i] = (response[byteIndex] & (1 << bitIndex)) != 0;
            }
            return discrreteIOs;
        }

        public ushort[] ReadHoldingRegisters(ushort startAddress, ushort numRegisters)
        {
            byte[] request = BuildRequest(0x03, startAddress, numRegisters);
            _stream.Write(request, 0, request.Length);

            byte[] response = new byte[9 + numRegisters * 2];
            _stream.Read(response, 0, response.Length);


            return ProcessResponse(response, numRegisters);
        }
        public ushort[] ReadInputRegisters(ushort startAddress, ushort numRegisters)
        {
            byte[] request = BuildRequest(0x04, startAddress, numRegisters);
            _stream.Write(request, 0, request.Length);

            byte[] response = new byte[9 + numRegisters * 2];
            _stream.Read(response, 0, response.Length);

            return ProcessResponse(response, numRegisters);
        }
        public bool[] ReadCoils(ushort startAddress, ushort numCoils)
        {
            byte[] request = BuildRequest(0x01, startAddress, numCoils);
            _stream.Write(request, 0, request.Length);

            int byteCount = (numCoils + 7) / 8;
            byte[] response = new byte[9 + byteCount];
            _stream.Read(response, 0, response.Length);

            return ProcessResponseDiscrete(response, numCoils);
        }
        public bool[] ReadDiscreteInputs(ushort startAddress, ushort numInputs)
        {
            byte[] request = BuildRequest(0x02, startAddress, numInputs);
            _stream.Write(request, 0, request.Length);

            int byteCount = (numInputs + 7) / 8;
            byte[] response = new byte[9 + byteCount];
            _stream.Read(response, 0, response.Length);

            return ProcessResponseDiscrete(response, numInputs);
        }
        // ------------------- WRITE FUNCTIONS -------------------
        public bool[] WriteSingleRegister(ushort address, object value)
        {
            return WriteSingleRegister(address, Convert.ToInt16(value));
        }
        public ushort[] WriteSingleRegister(ushort address, ushort value)
        {
            byte[] request = new byte[12];
            request[0] = 0x00; request[1] = 0x01; // Transaction ID
            request[2] = 0x00; request[3] = 0x00; // Protocol ID
            request[4] = 0x00; request[5] = 0x06; // Length
            request[6] = _slaveId;
            request[7] = 0x06; // Function Code
            request[8] = (byte)(address >> 8);
            request[9] = (byte)(address & 0xFF);
            request[10] = (byte)(value >> 8);
            request[11] = (byte)(value & 0xFF);

            _stream.Write(request, 0, request.Length);
            byte[] response = new byte[12];
            _stream.Read(response, 0, response.Length);
            return ProcessResponse(response, 1);
        }
        public bool[] WriteSingleCoil(ushort address, object value)
        {
            return WriteSingleCoil(address, Convert.ToBoolean(value));
        }
        public bool[] WriteSingleCoil(ushort address, bool value)
        {
            byte[] request = new byte[12];
            request[0] = 0x00; request[1] = 0x01;
            request[2] = 0x00; request[3] = 0x00;
            request[4] = 0x00; request[5] = 0x06;
            request[6] = _slaveId;
            request[7] = 0x05; // Function Code
            request[8] = (byte)(address >> 8);
            request[9] = (byte)(address & 0xFF);
            request[10] = value ? (byte)0xFF : (byte)0x00;
            request[11] = 0x00;

            _stream.Write(request, 0, request.Length);
            byte[] response = new byte[12];
            _stream.Read(response, 0, response.Length);
            return ProcessResponseDiscrete(response, 1);
        }



        ushort[] WriteMultipleRegisters(ushort startAddress, ushort[] values)
        {
            // Build Modbus TCP frame
            ushort transactionId = 1;
            byte functionCode = 16; // Write Multiple Registers
            ushort quantity = (ushort)values.Length;
            byte byteCount = (byte)(quantity * 2);

            // MBAP Header: Transaction ID (2), Protocol ID (2), Length (2), Unit ID (1)
            byte[] header = new byte[7];
            header[0] = (byte)(transactionId >> 8);
            header[1] = (byte)(transactionId & 0xFF);
            header[2] = 0; // Protocol ID high
            header[3] = 0; // Protocol ID low
            header[4] = (byte)((7 + byteCount) >> 8); // Length high
            header[5] = (byte)((7 + byteCount) & 0xFF); // Length low
            header[6] = _slaveId;

            // PDU: Function Code + Start Address + Quantity + Byte Count + Values
            byte[] pdu = new byte[6 + byteCount];
            pdu[0] = functionCode;
            pdu[1] = (byte)(startAddress >> 8);
            pdu[2] = (byte)(startAddress & 0xFF);
            pdu[3] = (byte)(quantity >> 8);
            pdu[4] = (byte)(quantity & 0xFF);
            pdu[5] = byteCount;

            // Add register values
            for (int i = 0; i < quantity; i++)
            {
                pdu[6 + i * 2] = (byte)(values[i] >> 8);
                pdu[7 + i * 2] = (byte)(values[i] & 0xFF);
            }

            // Combine header and PDU
            byte[] frame = new byte[header.Length + pdu.Length];
            Buffer.BlockCopy(header, 0, frame, 0, header.Length);
            Buffer.BlockCopy(pdu, 0, frame, header.Length, pdu.Length);

            // Send frame
            _stream.Write(frame, 0, frame.Length);

            // Read response
            byte[] response = new byte[12];
            int bytesRead = _stream.Read(response, 0, response.Length);
            return ProcessResponse(response, quantity);
        }


        #region MobusRequestStreamFunctions
        byte[] BuildRequest(byte functionCode, ushort startAddress, ushort quantity)
        {
            byte[] request = new byte[12];
            request[0] = 0x00; request[1] = 0x01; // Transaction ID
            request[2] = 0x00; request[3] = 0x00; // Protocol ID
            request[4] = 0x00; request[5] = 0x06; // Length
            request[6] = _slaveId;
            request[7] = functionCode;
            request[8] = (byte)(startAddress >> 8);
            request[9] = (byte)(startAddress & 0xFF);
            request[10] = (byte)(quantity >> 8);
            request[11] = (byte)(quantity & 0xFF);
            return request;
        }
        byte[] GetReadHoldingRegisterRequestArray(ushort startAddress, ushort numRegisters)
        {
            byte[] request = BuildRequest(0x03, startAddress, numRegisters);

            return request;
        }

        byte[] GetReadInputRegisterRequestArray(ushort startAddress, ushort numRegisters)
        {
            byte[] request = BuildRequest(0x04, startAddress, numRegisters);
            return request;
        }

        byte[] GetReadCoilsRequestArray(ushort startAddress, ushort numCoils)
        {
            byte[] request = BuildRequest(0x01, startAddress, numCoils);

            return request;
        }

        byte[] GetReadDiscreteInputsRequestArray(ushort startAddress, ushort numInputs)
        {
            byte[] request = BuildRequest(0x02, startAddress, numInputs);

            return request;
        }

        // ------------------- WRITE FUNCTIONS -------------------

        byte[] GetWriteSingleRegisterRequestArray(ushort address, ushort value)
        {
            byte[] request = new byte[12];
            request[0] = 0x00; request[1] = 0x01; // Transaction ID
            request[2] = 0x00; request[3] = 0x00; // Protocol ID
            request[4] = 0x00; request[5] = 0x06; // Length
            request[6] = _slaveId;
            request[7] = 0x06; // Function Code
            request[8] = (byte)(address >> 8);
            request[9] = (byte)(address & 0xFF);
            request[10] = (byte)(value >> 8);
            request[11] = (byte)(value & 0xFF);
            return request;
        }

        byte[] GetWriteSingleCoilRequestArray(ushort address, bool value)
        {
            byte[] request = new byte[12];
            request[0] = 0x00; request[1] = 0x01;
            request[2] = 0x00; request[3] = 0x00;
            request[4] = 0x00; request[5] = 0x06;
            request[6] = _slaveId;
            request[7] = 0x05; // Function Code
            request[8] = (byte)(address >> 8);
            request[9] = (byte)(address & 0xFF);
            request[10] = value ? (byte)0xFF : (byte)0x00;
            request[11] = 0x00;

            return request;
        }
        #endregion MobusRequestStreamFunctions


        public static byte[] ConvertBitsToBytes(bool[] bits)
        {
            int byteCount = (bits.Length + 7) / 8; // Round up to full bytes
            byte[] bytes = new byte[byteCount];

            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    bytes[i / 8] |= (byte)(1 << (7 - (i % 8))); // MSB first
                }
            }

            return bytes;
        }

        // New helper: checks response for Modbus exception and throws ModbusException if present
        void CheckForModbusException(byte[] response)
        {
            if (response == null || response.Length < 8)
                return; // nothing to check or invalid - callers already validate length

            byte functionCode = response[7];
            // If MSB is set in function code, the server returned an exception
            if ((functionCode & 0x80) != 0)
            {
                byte exceptionCode = response.Length > 8 ? response[8] : (byte)0x00;
                string description = MapExceptionCode(exceptionCode);
                throw new ModbusException($"Modbus exception from slave {_slaveId}: Function 0x{functionCode:X2}, Exception Code {exceptionCode} ({description}).");
            }
        }

        // Map known Modbus exception codes to readable messages
        string MapExceptionCode(byte code)
        {
            return code switch
            {
                1 => "Illegal Function",
                2 => "Illegal Data Address",
                3 => "Illegal Data Value",
                4 => "Slave Device Failure",
                5 => "Acknowledge",
                6 => "Slave Device Busy",
                8 => "Memory Parity Error",
                10 => "Gateway Path Unavailable",
                11 => "Gateway Target Device Failed to Respond",
                _ => "Unknown Modbus exception"
            };
        }
    }

    // Custom exception to represent Modbus errors returned by the slave
    public class ModbusException : Exception
    {
        public ModbusException() { }
        public ModbusException(string message) : base(message) { }
        public ModbusException(string message, System.Exception inner) : base(message, inner) { }
    }
}