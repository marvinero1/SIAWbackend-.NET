using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

public class CustomAuthenticationBehaviour : IEndpointBehavior
{
    private readonly string _authToken;

    public CustomAuthenticationBehaviour(string authToken)
    {
        _authToken = authToken;
    }

    public void Validate(ServiceEndpoint endpoint)
    {
    }

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
    }

    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
    {
    }

    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
    {
        // clientRuntime.ClientMessageInspectors.Add(new CustomMessageInspector(_authToken));
        clientRuntime.ClientMessageInspectors.Add(new CustomMessageInspector(_authToken));
    }
}
