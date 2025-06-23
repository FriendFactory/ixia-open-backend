FROM ubuntu:20.04

RUN apt-get update && \
    apt-get install -y curl && \
    apt-get install -y unzip && \
    apt-get install -y less && \
    apt-get install -y openssh-client

RUN curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip" && \
    unzip awscliv2.zip && \
    ./aws/install

RUN apt-get install -y gnupg2 && \
    apt install -y postgresql-common  && \
    /usr/share/postgresql-common/pgdg/apt.postgresql.org.sh -y && \
    apt-get install -y postgresql-client-16

RUN mkdir /root/.ssh
COPY clone.sh /root/
COPY sql-scripts/ /root/

WORKDIR /root