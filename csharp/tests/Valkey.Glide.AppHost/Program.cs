using Valkey.Glide.AppHost;

var builder = DistributedApplication.CreateBuilder(args);


var singleValkey = builder.AddValkeySingle("valkey-single");
var nodeMasterValkey = builder.AddValkeyCluster("valkey-node-master");
var nodeAValkey = builder.AddValkeyCluster("valkey-node-a")
    .AddMasterReference(nodeMasterValkey)
    .WaitFor(nodeMasterValkey);
var nodeBValkey = builder.AddValkeyCluster("valkey-node-b")
    .AddMasterReference(nodeMasterValkey)
    .WaitFor(nodeMasterValkey);
var nodeCValkey = builder.AddValkeyCluster("valkey-node-c")
    .AddMasterReference(nodeMasterValkey)
    .WaitFor(nodeMasterValkey);

builder.Build()
    .Run();
