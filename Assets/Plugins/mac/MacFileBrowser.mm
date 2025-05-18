#import <Cocoa/Cocoa.h>
#include <string.h>

extern "C" __attribute__((visibility("default"))) const char* _OpenFilePanel(const char* title, const char* directory, const char* extension, bool multiselect)
{
    @autoreleasepool {
        NSOpenPanel* panel = [NSOpenPanel openPanel];
        [panel setTitle:[NSString stringWithUTF8String:title]];
        [panel setAllowsMultipleSelection:multiselect];
        [panel setCanChooseFiles:YES];
        [panel setCanChooseDirectories:NO];

        if (extension != NULL && strlen(extension) > 0) {
            NSString* ext = [NSString stringWithUTF8String:extension];
            [panel setAllowedFileTypes:@[ext]];
        }

        NSInteger result = [panel runModal];
        if (result == NSModalResponseOK) {
            NSURL* url = [[panel URLs] objectAtIndex:0];
            return strdup([[url path] UTF8String]);
        }
        return "";
    }
}

extern "C" __attribute__((visibility("default"))) const char* _OpenFolderPanel(const char* title, const char* directory)
{
    @autoreleasepool {
        NSOpenPanel* panel = [NSOpenPanel openPanel];
        [panel setTitle:[NSString stringWithUTF8String:title]];
        // Disallow file selection and allow directory selection
        [panel setCanChooseFiles:NO];
        [panel setCanChooseDirectories:YES];
        [panel setAllowsMultipleSelection:NO];
        
        NSInteger result = [panel runModal];
        if (result == NSModalResponseOK) {
            NSURL* url = [[panel URLs] objectAtIndex:0];
            return strdup([[url path] UTF8String]);
        }
        return "";
    }
}