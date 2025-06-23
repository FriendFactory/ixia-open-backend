FROM ubuntu:bionic

RUN apt-get update && \ 
    apt install --assume-yes apt-utils && \
    apt-get install --assume-yes curl && \
    apt-get install --assume-yes jq && \
    apt-get install --assume-yes unzip

RUN curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip" && \
    unzip awscliv2.zip && \
    ./aws/install

WORKDIR /app/

COPY ./video-trending-api-collector.sh ./video-trending-api-collector.sh


CMD ["/app/video-trending-api-collector.sh"]