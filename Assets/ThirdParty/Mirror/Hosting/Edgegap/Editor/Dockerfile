FROM ubuntu:22.04

ARG DEBIAN_FRONTEND=noninteractive
ARG SERVER_BUILD_PATH=Builds/EdgegapServer

COPY ${SERVER_BUILD_PATH} /root/build/

WORKDIR /root/

RUN chmod +x /root/build/ServerBuild

RUN apt-get update && \
    apt-get install -y ca-certificates && \
    apt-get clean && \
    update-ca-certificates

# No "env;" before the server: it printed MatchKey and the other deployment secrets into the container log.
CMD ["/bin/bash", "-c", "/root/build/ServerBuild -batchmode -nographics $UNITY_COMMANDLINE_ARGS"]
