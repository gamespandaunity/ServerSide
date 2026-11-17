FROM ubuntu:22.04

ENV MALLOC_ARENA_MAX=2
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates libgcc-s1 libstdc++6 \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --uid 10001 unity

WORKDIR /app
ARG SERVER_BUILD_PATH=Builds/MemoryServer
COPY --chown=unity:unity ${SERVER_BUILD_PATH}/ ./
RUN chmod +x ServerBuild \
    && printf '\njob-worker-count=2\ngc-helper-count=2\nmemorysetup-main-allocator-block-size=2097152\nmemorysetup-thread-allocator-block-size=2097152\nmemorysetup-gfx-main-allocator-block-size=1048576\nmemorysetup-gfx-thread-allocator-block-size=1048576\nmemorysetup-cache-allocator-block-size=1048576\nmemorysetup-typetree-allocator-block-size=1048576\nmemorysetup-bucket-allocator-block-size=1048576\nmemorysetup-profiler-allocator-block-size=1048576\nmemorysetup-profiler-bucket-allocator-block-size=1048576\nmemorysetup-temp-allocator-size-main=1048576\nmemorysetup-job-temp-allocator-block-size=524288\nmemorysetup-job-temp-allocator-block-size-background=524288\n' >> ServerBuild_Data/boot.config

USER unity
CMD ["/bin/sh", "-c", "exec /app/ServerBuild -batchmode -nographics -logFile - $UNITY_COMMANDLINE_ARGS"]
