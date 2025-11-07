#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>

@interface TextToSpeechBridge : NSObject <AVSpeechSynthesizerDelegate>
@property (nonatomic, strong) AVSpeechSynthesizer *synthesizer;
@property (nonatomic, copy) void (^completionHandler)(void);
@end

@implementation TextToSpeechBridge

- (instancetype)init {
    self = [super init];
    if (self) {
        self.synthesizer = [[AVSpeechSynthesizer alloc] init];
        self.synthesizer.delegate = self;
    }
    return self;
}

- (void)speak:(NSString *)text completion:(void (^)(void))completion {
    NSLog(@"TTS: Speaking text: %@", text);

    self.completionHandler = completion;

    // Create speech utterance
    AVSpeechUtterance *utterance = [[AVSpeechUtterance alloc] initWithString:text];

    // Try to use kid-friendly voices in order of preference
    AVSpeechSynthesisVoice *voice = nil;

    // Option 1: Try Samantha (friendly, clear, natural)
    voice = [AVSpeechSynthesisVoice voiceWithIdentifier:@"com.apple.ttsbundle.Samantha-compact"];

    // Option 2: Try Karen (Australian, friendly)
    if (!voice) {
        voice = [AVSpeechSynthesisVoice voiceWithIdentifier:@"com.apple.ttsbundle.Karen-compact"];
    }

    // Option 3: Try Zoe (compact, clear)
    if (!voice) {
        voice = [AVSpeechSynthesisVoice voiceWithIdentifier:@"com.apple.voice.compact.en-US.Samantha"];
    }

    // Option 4: Fallback to default US English
    if (!voice) {
        voice = [AVSpeechSynthesisVoice voiceWithLanguage:@"en-US"];
    }

    utterance.voice = voice;
    NSLog(@"TTS: Using voice: %@", voice.name ?: @"default");

    // Set speech parameters for a friendly character
    utterance.rate = 0.48f; // Slightly slower for clarity (0.5 = normal, good for kids)
    utterance.pitchMultiplier = 1.15f; // Slightly higher pitch for friendly character
    utterance.volume = 1.0f; // Full volume

    // Speak
    [self.synthesizer speakUtterance:utterance];
}

- (void)stop {
    NSLog(@"TTS: Stopping speech");
    [self.synthesizer stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
}

// AVSpeechSynthesizerDelegate methods
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer didFinishSpeechUtterance:(AVSpeechUtterance *)utterance {
    NSLog(@"TTS: Finished speaking");
    if (self.completionHandler) {
        self.completionHandler();
        self.completionHandler = nil;
    }
}

- (void)speechSynthesizer:(AVSpeechSynthesizer *)synthesizer didCancelSpeechUtterance:(AVSpeechUtterance *)utterance {
    NSLog(@"TTS: Speech cancelled");
    if (self.completionHandler) {
        self.completionHandler();
        self.completionHandler = nil;
    }
}

@end

// Global instance
static TextToSpeechBridge *ttsInstance = nil;

// Callback type for completion
typedef void (*TTSCompletionCallback)(void);
static TTSCompletionCallback globalCompletionCallback = NULL;

extern "C" {
    // Initialize TTS
    void _InitTextToSpeech() {
        if (!ttsInstance) {
            ttsInstance = [[TextToSpeechBridge alloc] init];
            NSLog(@"TTS: Initialized");
        }
    }

    // Speak text with completion callback
    void _Speak(const char* text, TTSCompletionCallback callback) {
        if (!ttsInstance) {
            _InitTextToSpeech();
        }

        NSString *textStr = [NSString stringWithUTF8String:text];
        globalCompletionCallback = callback;

        [ttsInstance speak:textStr completion:^{
            if (globalCompletionCallback) {
                globalCompletionCallback();
            }
        }];
    }

    // Stop speaking
    void _StopSpeaking() {
        if (ttsInstance) {
            [ttsInstance stop];
        }
    }

    // Check if currently speaking
    BOOL _IsSpeaking() {
        if (ttsInstance) {
            return ttsInstance.synthesizer.isSpeaking;
        }
        return NO;
    }
}
