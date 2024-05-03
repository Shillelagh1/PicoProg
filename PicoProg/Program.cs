using MCP2210_TOOLS;

namespace PicoProg
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No args.\n");
                return;
            }
            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"No such file '{args[0]}'.\n");
                return;
            }

            // Open and read the file
            Console.WriteLine($">> Reading bytestream from '{args[0]}'.");
            var f = File.Open(args[0], FileMode.Open);
            byte[] bytestream = new byte[f.Length];
            f.Read(bytestream, 0, (int)f.Length);
            Console.WriteLine($">> {bytestream.Length} Bytes loaded.");
            Console.WriteLine(">> Done.\n");
            f.Close();

            // Open the board
            FTDI_2210 FTDI = new FTDI_2210();
            FTDI.open();

            AwaitIdle(FTDI);

            // Reset the ICE40
            FTDI.setPin(2, false);

            Console.WriteLine(">> Erasing EEPROM...");

            // Clear the Flash EEPROM
            FTDI.setPin(0, false);
            FTDI.xfer(0x06);
            FTDI.xfer(0x60);
            FTDI.setPin(0, true);
            AwaitIdle(FTDI);
            Console.WriteLine(">> Done.\n");

            Console.WriteLine($">> Writing {bytestream.Length} bytes to FPGA...");
            int j;
            int k = 0;
            for (int i = 0; i < bytestream.Length; i += 256)
            {
                // Enable Writing
                FTDI.setPin(0, false);
                FTDI.xfer(0x06);
                FTDI.setPin(0, true);
                AwaitIdle(FTDI);

                // Write a full page
                var tx = new List<byte>()
                {
                    0x02, (byte)((i>>16)&255), (byte)((i>>8)&255), (byte)(i&255)
                };
                for (j = i; j < Math.Min(i+256, bytestream.Length); j++)
                {
                    tx.Add(bytestream[j]);
                }
                Console.Write($">> Page {k}: ");
                FTDI.setPin(0, false);
                FTDI.xfer(tx.ToArray());
                FTDI.setPin(0, true);
                AwaitIdle(FTDI);
                Console.WriteLine("Done.");
                k++;
            }

            Console.WriteLine("\n>> Done!\n");
        }

        static void AwaitIdle(FTDI_2210 FTDI)
        {
            DateTime _s;
            while (true)
            {
                if ((FTDI.xfer(0x05, 0x00)[1] & 1) == 0) break;


                _s = DateTime.Now;
                while (DateTime.Now.Subtract(_s).TotalSeconds < 2);
            }
        }
    }
}
