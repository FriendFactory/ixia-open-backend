# inspired by https://github.com/hauptmedia/docker-jmeter  and
# https://github.com/hhcordero/docker-jmeter-server/blob/master/Dockerfile
FROM alpine:3.12

ARG JMETER_VERSION="5.4.1"
ENV JMETER_HOME /opt/apache-jmeter-${JMETER_VERSION}
ENV	JMETER_BIN	${JMETER_HOME}/bin
ENV	JMETER_DOWNLOAD_URL  https://archive.apache.org/dist/jmeter/binaries/apache-jmeter-${JMETER_VERSION}.tgz

# Install extra packages
# Set TimeZone, See: https://github.com/gliderlabs/docker-alpine/issues/136#issuecomment-612751142
ARG TZ="Europe/Amsterdam"
ENV TZ ${TZ}
RUN    apk update \
    && apk upgrade \
    && apk add ca-certificates \
    && update-ca-certificates \
    && apk add --update openjdk8-jre tzdata curl unzip bash \
    && apk add --no-cache nss \
    && rm -rf /var/cache/apk/* \
    && mkdir -p /tmp/dependencies  \
    && curl -L --silent ${JMETER_DOWNLOAD_URL} >  /tmp/dependencies/apache-jmeter-${JMETER_VERSION}.tgz  \
    && mkdir -p /opt  \
    && tar -xzf /tmp/dependencies/apache-jmeter-${JMETER_VERSION}.tgz -C /opt  \
    && rm -rf /tmp/dependencies

RUN apk add --no-cache \
    python3 \
    py3-pip \
    && pip3 install --upgrade pip \
    && pip3 install \
    awscli \
    && rm -rf /var/cache/apk/*

ENV KUBE_LATEST_VERSION="v1.21.1"
RUN export ARCH="$(uname -m)" && if [[ ${ARCH} == "x86_64" ]]; then export ARCH="amd64"; fi && curl -L https://storage.googleapis.com/kubernetes-release/release/${KUBE_LATEST_VERSION}/bin/linux/${ARCH}/kubectl -o /usr/local/bin/kubectl \
    && chmod +x /usr/local/bin/kubectl

# Set global PATH such that "jmeter" command is found
ENV PATH $PATH:$JMETER_BIN

# Entrypoint has same signature as "jmeter" command
COPY run-test.sh /
COPY plugins/ ${JMETER_HOME}/lib/ext
COPY rmi_keystore.jks ${JMETER_BIN}
COPY rmi_keystore.jks ${JMETER_HOME}

RUN chmod +x /run-test.sh

USER root
WORKDIR	${JMETER_HOME}

EXPOSE 1099

#CMD ["kubectl", "version", "--client"]
#CMD ["ls", "/opt/apache-jmeter-5.3/bin", "-A1"]
ENTRYPOINT ["/run-test.sh"]
