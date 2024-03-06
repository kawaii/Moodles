using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.Data;
[MemoryPackable]
public partial record struct IncomingMessage
{
    public string From = "";
    public string To = "";
    public List<MyStatus> ApplyStatuses = [];

    [MemoryPackConstructor]
    public IncomingMessage()
    {
    }

    public IncomingMessage(string from, string to, List<MyStatus> applyStatuses)
    {
        this.From = from;
        this.To = to;
        this.ApplyStatuses = applyStatuses;
    }

    public readonly byte[] Serialize()
    {
        return MemoryPackSerializer.Serialize(this);
    }

    public static bool TryDeserialize(byte[] data, out IncomingMessage message)
    {
        try
        {
            message = MemoryPackSerializer.Deserialize<IncomingMessage>(data);
            return true;
        }
        catch(Exception e)
        {
            e.Log();
            message = default;
            return false;
        }
    }
}
