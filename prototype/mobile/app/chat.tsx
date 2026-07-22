import React, { useState, useRef } from 'react';
import {
  View, Text, TextInput, StyleSheet, FlatList,
  TouchableOpacity, KeyboardAvoidingView, Platform, ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { api } from '../services/api';

interface Message {
  id: string;
  role: 'user' | 'assistant';
  text: string;
}

export default function ChatScreen() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);
  const flatListRef = useRef<FlatList>(null);

  const send = async () => {
    if (!input.trim() || sending) return;
    const userMsg: Message = { id: Date.now().toString(), role: 'user', text: input.trim() };
    setMessages(prev => [...prev, userMsg]);
    setInput('');
    setSending(true);

    try {
      const data = await api.chat(userMsg.text);
      const assistantMsg: Message = { id: (Date.now() + 1).toString(), role: 'assistant', text: data.response };
      setMessages(prev => [...prev, assistantMsg]);
    } catch {
      const errMsg: Message = { id: (Date.now() + 1).toString(), role: 'assistant', text: 'Sorry, I had trouble reaching the server. Make sure the API key is configured.' };
      setMessages(prev => [...prev, errMsg]);
    } finally {
      setSending(false);
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <Text style={styles.heading}>🤖 Chat Assistant</Text>
      <FlatList
        ref={flatListRef}
        data={messages}
        keyExtractor={(m) => m.id}
        contentContainerStyle={styles.chatList}
        onContentSizeChange={() => flatListRef.current?.scrollToEnd({ animated: true })}
        renderItem={({ item }) => (
          <View style={[styles.bubble, item.role === 'user' ? styles.userBubble : styles.assistantBubble]}>
            <Text style={styles.bubbleText}>{item.text}</Text>
          </View>
        )}
        ListEmptyComponent={
          <View style={styles.emptyState}>
            <Text style={styles.emptyIcon}>🧑‍🍳</Text>
            <Text style={styles.emptyText}>Ask me about your fridge!</Text>
            <Text style={styles.emptyHint}>"What can I cook with what's in my fridge?"</Text>
            <Text style={styles.emptyHint}>"Plan my meals for the next 3 days"</Text>
            <Text style={styles.emptyHint}>"What's expiring soon?"</Text>
          </View>
        }
      />
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={styles.inputRow}>
          <TextInput
            style={styles.input}
            placeholder="Ask about your meals..."
            value={input}
            onChangeText={setInput}
            onSubmitEditing={send}
            returnKeyType="send"
          />
          <TouchableOpacity style={styles.sendBtn} onPress={send} disabled={sending}>
            {sending ? (
              <ActivityIndicator color="#FFF" size="small" />
            ) : (
              <Text style={styles.sendText}>Send</Text>
            )}
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF8F0' },
  heading: { fontSize: 26, fontWeight: '700', color: '#E67E22', padding: 20, paddingBottom: 10 },
  chatList: { paddingHorizontal: 14, paddingBottom: 10 },
  bubble: {
    maxWidth: '80%', padding: 12, borderRadius: 16, marginBottom: 10,
  },
  userBubble: { alignSelf: 'flex-end', backgroundColor: '#E67E22' },
  assistantBubble: { alignSelf: 'flex-start', backgroundColor: '#FFF', borderWidth: 1, borderColor: '#F0E0D0' },
  bubbleText: { fontSize: 14, lineHeight: 20, color: '#2C3E50' },
  emptyState: { alignItems: 'center', marginTop: 60 },
  emptyIcon: { fontSize: 48, marginBottom: 12 },
  emptyText: { fontSize: 16, fontWeight: '600', color: '#2C3E50', marginBottom: 8 },
  emptyHint: { fontSize: 13, color: '#95A5A6', marginBottom: 4 },
  inputRow: {
    flexDirection: 'row', padding: 10, gap: 8, borderTopWidth: 1,
    borderTopColor: '#F0E0D0', backgroundColor: '#FFF',
  },
  input: {
    flex: 1, backgroundColor: '#F8F9FA', borderRadius: 20, paddingHorizontal: 16,
    paddingVertical: 10, fontSize: 14, borderWidth: 1, borderColor: '#E9ECEF',
  },
  sendBtn: {
    backgroundColor: '#E67E22', borderRadius: 20, paddingHorizontal: 18,
    alignItems: 'center', justifyContent: 'center',
  },
  sendText: { color: '#FFF', fontWeight: '600', fontSize: 14 },
});
