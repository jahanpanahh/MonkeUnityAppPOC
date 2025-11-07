#import <Foundation/Foundation.h>
#import <Speech/Speech.h>

// Callback type for sending recognized text back to Unity
typedef void (*RecognitionCallback)(const char* text);

@interface SpeechRecognizerBridge : NSObject
@property (nonatomic, strong) SFSpeechRecognizer *speechRecognizer;
@property (nonatomic, strong) SFSpeechAudioBufferRecognitionRequest *recognitionRequest;
@property (nonatomic, strong) SFSpeechRecognitionTask *recognitionTask;
@property (nonatomic, strong) AVAudioEngine *audioEngine;
@property (nonatomic, assign) RecognitionCallback callback;
@property (nonatomic, strong) NSString *latestTranscription;
@property (nonatomic, strong) NSTimer *silenceTimer;
@property (nonatomic, assign) BOOL hasReceivedTranscription;
- (void)onSilenceTimeout;
@end

@implementation SpeechRecognizerBridge

- (instancetype)init {
    self = [super init];
    if (self) {
        self.speechRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:[NSLocale localeWithLocaleIdentifier:@"en-US"]];
        self.audioEngine = [[AVAudioEngine alloc] init];
    }
    return self;
}

- (void)requestPermission:(void(^)(BOOL authorized))completion {
    [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
        dispatch_async(dispatch_get_main_queue(), ^{
            BOOL isAuthorized = (status == SFSpeechRecognizerAuthorizationStatusAuthorized);
            completion(isAuthorized);
        });
    }];
}

- (BOOL)startRecording {
    // Clean up any previous session completely
    if (self.audioEngine.isRunning) {
        [self.audioEngine stop];
    }

    if (self.recognitionTask) {
        [self.recognitionTask cancel];
        self.recognitionTask = nil;
    }

    if (self.recognitionRequest) {
        [self.recognitionRequest endAudio];
        self.recognitionRequest = nil;
    }

    // Clear previous transcription and timer
    self.latestTranscription = nil;
    self.hasReceivedTranscription = NO;

    // Cancel any existing timer
    if (self.silenceTimer) {
        [self.silenceTimer invalidate];
        self.silenceTimer = nil;
    }

    // Configure audio session - use simpler configuration
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    // Try to set category, but don't fail if Unity already configured it
    [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord mode:AVAudioSessionModeDefault options:AVAudioSessionCategoryOptionDefaultToSpeaker error:&error];
    if (error) {
        NSLog(@"Audio session setCategory warning (may be OK if Unity already configured): %@", error);
        error = nil; // Clear error and continue
    }

    [audioSession setActive:YES error:&error];
    if (error) {
        NSLog(@"Audio session setActive warning: %@", error);
        error = nil; // Clear error and continue
    }

    // Create recognition request
    self.recognitionRequest = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
    self.recognitionRequest.shouldReportPartialResults = YES; // Enable partial results for better detection

    // Get input node
    AVAudioInputNode *inputNode = self.audioEngine.inputNode;
    SFSpeechAudioBufferRecognitionRequest *request = self.recognitionRequest;
    RecognitionCallback callback = self.callback;

    // Start recognition task
    self.recognitionTask = [self.speechRecognizer recognitionTaskWithRequest:self.recognitionRequest resultHandler:^(SFSpeechRecognitionResult *result, NSError *error) {
        BOOL isFinal = NO;

        if (result) {
            // Store the latest transcription
            self.latestTranscription = result.bestTranscription.formattedString;
            self.hasReceivedTranscription = YES;
            isFinal = result.isFinal;
            NSLog(@"Transcription (isFinal=%d): %@", isFinal, self.latestTranscription);

            // Reset the silence timer on each new transcription
            if (self.silenceTimer) {
                [self.silenceTimer invalidate];
            }
            // Start a 2-second timer - if no new transcription comes, auto-stop
            self.silenceTimer = [NSTimer scheduledTimerWithTimeInterval:2.0
                                                                 target:self
                                                               selector:@selector(onSilenceTimeout)
                                                               userInfo:nil
                                                                repeats:NO];

            // If this is the final result (user stopped speaking), send it automatically
            // But only if we haven't already sent it via the timer
            if (isFinal && callback && self.latestTranscription.length > 0 && self.silenceTimer != nil) {
                NSLog(@"Auto-detected end of speech (isFinal), sending: %@", self.latestTranscription);
                [self.silenceTimer invalidate];
                self.silenceTimer = nil;
                callback([self.latestTranscription UTF8String]);
                self.latestTranscription = nil;
            } else if (isFinal && self.latestTranscription.length == 0) {
                NSLog(@"Got isFinal with empty transcription, ignoring");
            }
        }

        if (error || isFinal) {
            NSLog(@"Stopping recognition (error=%@, isFinal=%d)", error, isFinal);
            [self.audioEngine stop];
            [inputNode removeTapOnBus:0];
            self.recognitionRequest = nil;
            self.recognitionTask = nil;
            if (self.silenceTimer) {
                [self.silenceTimer invalidate];
                self.silenceTimer = nil;
            }
        }
    }];

    // Configure microphone input
    AVAudioFormat *recordingFormat = [inputNode outputFormatForBus:0];
    NSLog(@"Recording format: %@", recordingFormat);
    [inputNode installTapOnBus:0 bufferSize:1024 format:recordingFormat block:^(AVAudioPCMBuffer *buffer, AVAudioTime *when) {
        if (request) {
            [request appendAudioPCMBuffer:buffer];
        }
    }];

    // Start audio engine
    [self.audioEngine prepare];
    [self.audioEngine startAndReturnError:&error];

    if (error) {
        NSLog(@"Audio engine error: %@", error);
        return NO;
    }

    return YES;
}

- (void)onSilenceTimeout {
    NSLog(@"Silence timeout - auto-stopping and sending transcription");

    // Send the transcription first before stopping everything
    if (self.latestTranscription && self.callback && self.latestTranscription.length > 0) {
        NSLog(@"Sending transcription after silence: %@", self.latestTranscription);
        self.callback([self.latestTranscription UTF8String]);
        self.latestTranscription = nil;
    }

    // Now stop recording
    if (self.audioEngine.inputNode && self.audioEngine.isRunning) {
        [self.audioEngine.inputNode removeTapOnBus:0];
    }

    if (self.audioEngine.isRunning) {
        [self.audioEngine stop];
    }

    if (self.recognitionRequest) {
        [self.recognitionRequest endAudio];
        self.recognitionRequest = nil;
    }

    if (self.recognitionTask) {
        [self.recognitionTask cancel];
        self.recognitionTask = nil;
    }

    self.silenceTimer = nil;
}

- (void)stopRecording {
    NSLog(@"stopRecording called (manual stop)");

    // Remove tap first before stopping engine
    if (self.audioEngine.inputNode && self.audioEngine.isRunning) {
        [self.audioEngine.inputNode removeTapOnBus:0];
    }

    // Stop audio engine
    if (self.audioEngine.isRunning) {
        [self.audioEngine stop];
    }

    // End audio input - this will trigger isFinal=true in the result handler
    if (self.recognitionRequest) {
        [self.recognitionRequest endAudio];
    }

    // Cancel recognition task after a delay to let it finalize
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.5 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        if (self.recognitionTask) {
            [self.recognitionTask cancel];
            self.recognitionTask = nil;
        }
    });
}

@end

// Global instance
static SpeechRecognizerBridge *sharedInstance = nil;

extern "C" {
    // Initialize speech recognizer
    void _InitSpeechRecognizer() {
        if (!sharedInstance) {
            sharedInstance = [[SpeechRecognizerBridge alloc] init];
        }
    }

    // Request speech recognition permission
    void _RequestSpeechPermission() {
        if (!sharedInstance) {
            _InitSpeechRecognizer();
        }

        [sharedInstance requestPermission:^(BOOL authorized) {
            if (authorized) {
                NSLog(@"Speech recognition authorized");
            } else {
                NSLog(@"Speech recognition not authorized");
            }
        }];
    }

    // Start recording and recognizing speech
    BOOL _StartSpeechRecognition(RecognitionCallback callback) {
        if (!sharedInstance) {
            _InitSpeechRecognizer();
        }

        sharedInstance.callback = callback;
        return [sharedInstance startRecording];
    }

    // Stop recording
    void _StopSpeechRecognition() {
        if (sharedInstance) {
            [sharedInstance stopRecording];
        }
    }
}
