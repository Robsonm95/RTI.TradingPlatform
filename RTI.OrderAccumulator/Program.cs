using RTI.OrderAccumulator.Fix;

Console.WriteLine("RTI Order Accumulator");

var acceptor = new FixAcceptor();

acceptor.Start();

Console.ReadLine();