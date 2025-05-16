CC=msbuild
TARGET=CustomImageMod.dll
SOURCES := $(shell find . -name '*.cs')

bin/Release/net472/$(TARGET): $(SOURCES)
	$(CC) /p:Configuration=Release

build: bin/Release/net472/$(TARGET)

clean:
	$(CC) /t:Clean

install:
	cp bin/Release/net472/$(TARGET) /home/midnight/.steam/steam/steamapps/common/Coloring\ Pixels/BepInEx/plugins/$(TARGET)
	cp bin/Release/net472/$(TARGET) /home/midnight/Documents/VM\ Shared\ Folder/$(TARGET)

stop:
	pkill -9 ColoringPixels || true

start:
	steam steam://rungameid/897330

run: stop clean bin/Release/net472/$(TARGET) install start

client: clean build
	cp bin/Release/netstandard2.0/$(TARGET) /home/midnight/Documents/VM\ Shared\ Folder/$(TARGET)

.PHONY: build clean install stop start run client