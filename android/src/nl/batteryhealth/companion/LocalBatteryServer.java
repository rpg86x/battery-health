package nl.batteryhealth.companion;

import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.util.concurrent.*;

/** Small read-only authenticated LAN endpoint, independent of Android for protocol tests. */
public final class LocalBatteryServer implements Closeable {
    public interface Source { String snapshot() throws Exception; }
    private final ServerSocket listener;
    private final String token;
    private final Source source;
    private final ThreadPoolExecutor clients;
    private final Set<Socket> sockets = Collections.newSetFromMap(new ConcurrentHashMap<Socket, Boolean>());
    private volatile boolean closed;
    public LocalBatteryServer(int port, String token, Source source) throws IOException {
        if (token == null || !token.matches("[a-f0-9]{32}")) throw new IllegalArgumentException("Invalid token");
        this.token = token; this.source = source;
        listener = new ServerSocket(); listener.setReuseAddress(true); listener.bind(new InetSocketAddress(port));
        clients = new ThreadPoolExecutor(2, 4, 30, TimeUnit.SECONDS, new ArrayBlockingQueue<Runnable>(8));
        Thread accept = new Thread(new Runnable() { public void run() { acceptClients(); } }, "battery-lan");
        accept.setDaemon(true); accept.start();
    }
    public int port() { return listener.getLocalPort(); }
    private void acceptClients() {
        while (!closed) {
            try {
                final Socket socket = listener.accept(); socket.setSoTimeout(3000);
                if (!localAddress(socket.getInetAddress())) { socket.close(); continue; }
                sockets.add(socket);
                try { clients.execute(new Runnable() { public void run() { handle(socket); } }); }
                catch (RejectedExecutionException busy) { sockets.remove(socket); socket.close(); }
            } catch (IOException ignored) { if (!closed) close(); }
        }
    }
    private static boolean localAddress(InetAddress address) {
        return address.isLoopbackAddress() || address.isSiteLocalAddress() || address.isLinkLocalAddress();
    }
    private static String line(InputStream in) throws IOException {
        ByteArrayOutputStream text = new ByteArrayOutputStream();
        for (int i = 0; i < 1024; i++) {
            int b = in.read(); if (b == -1) return null;
            if (b == '\n') return new String(text.toByteArray(), StandardCharsets.US_ASCII).trim();
            if (b != '\r') text.write(b);
        }
        throw new IOException("Request line too long");
    }
    private void handle(Socket socket) {
        try (Socket s = socket) {
            InputStream in = s.getInputStream(); String request = line(in), key = null;
            if (request == null) return;
            int total = request.length(); boolean ended = false;
            for (int i = 0; i < 64; i++) {
                String header = line(in); if (header == null) return;
                if (header.isEmpty()) { ended = true; break; }
                total += header.length(); if (total > 8192) return;
                int separator = header.indexOf(':');
                if (separator > 0 && header.substring(0, separator).equalsIgnoreCase("X-Battery-Key")) key = header.substring(separator + 1).trim();
            }
            if (!ended) return;
            if (key == null || !MessageDigest.isEqual(token.getBytes(StandardCharsets.US_ASCII), key.getBytes(StandardCharsets.US_ASCII))) { reply(s, "401 Unauthorized", "{\"error\":\"unauthorized\"}"); return; }
            if (!request.equals("GET /battery HTTP/1.1") && !request.equals("GET /battery HTTP/1.0")) { reply(s, "404 Not Found", "{\"error\":\"not_found\"}"); return; }
            try { reply(s, "200 OK", source.snapshot()); }
            catch (Exception unavailable) { reply(s, "503 Service Unavailable", "{\"error\":\"unavailable\"}"); }
        } catch (IOException ignored) { }
        finally { sockets.remove(socket); }
    }
    private static void reply(Socket socket, String status, String json) throws IOException {
        byte[] body = json.getBytes(StandardCharsets.UTF_8);
        String headers = "HTTP/1.1 " + status + "\r\nContent-Type: application/json; charset=utf-8\r\nCache-Control: no-store\r\nConnection: close\r\nContent-Length: " + body.length + "\r\n\r\n";
        OutputStream out = socket.getOutputStream(); out.write(headers.getBytes(StandardCharsets.US_ASCII)); out.write(body); out.flush();
    }
    public void close() {
        closed = true;
        try { listener.close(); } catch (IOException ignored) { }
        for (Socket socket : sockets) { try { socket.close(); } catch (IOException ignored) { } }
        sockets.clear();
        clients.shutdownNow();
    }
}
