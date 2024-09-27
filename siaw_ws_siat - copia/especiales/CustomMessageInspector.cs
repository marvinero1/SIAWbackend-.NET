using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

public class CustomMessageInspector : IClientMessageInspector
{
    private readonly string _authToken;

    public CustomMessageInspector(string authToken)
    {
        _authToken = authToken;
    }

    public object BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        var reqMsgProperty = new HttpRequestMessageProperty();
        reqMsgProperty.Headers.Add("apikey", _authToken);
        request.Properties[HttpRequestMessageProperty.Name] = reqMsgProperty;
        return null;
    }

    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
    }
}
