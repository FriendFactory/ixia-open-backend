#!/bin/bash

ENV=$1

# https://stackoverflow.com/questions/50028732/how-do-i-create-a-pfx-compatible-with-x509certificate2-with-openssl
# https://stackoverflow.com/questions/10175812/how-to-generate-a-self-signed-ssl-certificate-using-openssl?rq=1

openssl req -x509 -newkey rsa:2048 -keyout private.pem -out cert.pem -sha256 -days 3650 -nodes -subj "/C=SE/ST=SE/L=Stockholm/O=Frever/OU=Security/CN=frever.com"
openssl pkcs12 -in cert.pem -inkey private.pem -export -clcerts -out ${ENV}.pfx

rm private.pem
rm cert.pem