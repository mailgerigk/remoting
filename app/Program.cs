using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

public interface IData
{
    int Foo { get; set; }
}

public interface ILogic
{
    void Update(IData data);
}

class AssemblyLoader : MarshalByRefObject
{
    public Assembly Assembly { get; private set; }

    public AssemblyLoader(string assemblyFile)
    {
        Assembly = Assembly.LoadFrom(assemblyFile);
    }

    public object CreateInstance(string typeName)
    {
        return Activator.CreateInstance(Assembly.GetType(typeName));
    }
}

class ClientProxy : RealProxy
{
    private RealProxy innerProxy;

    public ClientProxy(Type interfaceType, object proxyObject)
        : base(interfaceType)
    {
        SetInnerProxy(proxyObject);
    }

    public void SetInnerProxy(object proxyObject)
    {
        innerProxy = RemotingServices.GetRealProxy(proxyObject);
    }

    public override IMessage Invoke(IMessage msg)
    {
        return innerProxy.Invoke(msg);
    }
}

class Program
{
    static void Main(string[] args)
    {
        var app = AppDomain.CreateDomain("ImplDomain", null,
            AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.RelativeSearchPath,
            true);

        var assmblyLoader = app.CreateInstanceFromAndUnwrap(
            typeof(AssemblyLoader).Assembly.Location, typeof(AssemblyLoader).FullName,
            false, BindingFlags.CreateInstance, null,
            new object[]
            {
                "_impl.dll"
            },
            null, null) as AssemblyLoader;

        var dataImpl = assmblyLoader.CreateInstance("DataImpl") as IData;
        var logicImpl = assmblyLoader.CreateInstance("LogicImpl") as ILogic;

        logicImpl.Update(dataImpl); // Works
        Console.WriteLine(dataImpl.Foo); // prints 1

        var clientDataProxy = new ClientProxy(typeof(IData), dataImpl);
        var clientDataImpl = clientDataProxy.GetTransparentProxy() as IData;

        var clientLogicProxy = new ClientProxy(typeof(ILogic), logicImpl);
        var clientLogicImpl = clientLogicProxy.GetTransparentProxy() as ILogic;

        clientLogicImpl.Update(dataImpl); // Works
        Console.WriteLine(clientDataImpl.Foo); // prints 2

        clientLogicImpl.Update(clientDataImpl); // throws System.Runtime.Remoting.RemotingException
        Console.WriteLine(clientDataImpl.Foo);
    }
}

