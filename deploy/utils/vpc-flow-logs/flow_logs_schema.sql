create table vpc_flow_logs (
    version int not null,
    account_id text not null,
    interface_id text not null,
    source_address cidr not null,
    destination_address cidr not null,
    source_port int not null,
    destination_port int not null,
    protocol int not null,
    packets int not null,
    bytes int not null,
    start_timestamp int not null,
    end_timestamp int not null,
    action text not null,
    status text not null
);

create index vpc_flow_logs__interface_id on vpc_flow_logs(interface_id);

create table known_hosts (
    id text not null primary key,
    name text not null,
    type text not null
);

create table known_hosts_ip (
    known_host_id text not null references known_hosts(id),
    ip cidr not null
);

create table network_interfaces (
    id text not null primary key ,
    description text null
);

create table network_interface_ip (
    network_interface_id text not null references network_interfaces(id),
    ip cidr not null
);

copy vpc_flow_logs(version, account_id, interface_id,
                   source_address, destination_address, source_port, destination_port, protocol,
                   packets, bytes, start_timestamp, end_timestamp, action, status)
from '/Users/sergiitokariev/dev/frever/Server/deploy/utils/.data/vpc-full.log'
delimiter ' ';