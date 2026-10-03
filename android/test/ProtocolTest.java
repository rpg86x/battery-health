import nl.batteryhealth.companion.LocalBatteryServer;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.concurrent.TimeUnit;
public class ProtocolTest {
    private static String request(int port, String path, String key) throws Exception {
        try (Socket socket = new Socket("127.0.0.1", port)) {
            socket.setSoTimeout(4000);
            String header = "GET " + path + " HTTP/1.1\r\nHost: localhost\r\n" + (key == null ? "" : "X-Battery-Key: " + key + "\r\n") + "\r\n";
            socket.getOutputStream().write(header.getBytes(StandardCharsets.US_ASCII));
            ByteArrayOutputStream response = new ByteArrayOutputStream(); byte[] buffer = new byte[1024]; int count;
            while ((count = socket.getInputStream().read(buffer)) != -1) response.write(buffer, 0, count);
            return new String(response.toByteArray(), StandardCharsets.UTF_8);
        }
    }
    public static void main(String[] args) throws Exception {
        String key = "0123456789abcdef0123456789abcdef";
        String sample = "{\"protocol\":1,\"identity\":\"12345678-1234-1234-1234-123456789abc\",\"level\":72,\"status\":3,\"health\":2,\"model\":\"Example only\"}";
        try (LocalBatteryServer server = new LocalBatteryServer(0, key, () -> sample)) {
            int port = server.port();
            String authorized = request(port, "/battery", key);
            if (!authorized.startsWith("HTTP/1.1 200 OK") || !authorized.endsWith(sample) || !authorized.contains("Cache-Control: no-store")) throw new Exception("Authorized response failed");
            if (!request(port, "/battery", null).startsWith("HTTP/1.1 401")) throw new Exception("Missing key accepted");
            if (!request(port, "/battery", "ffffffffffffffffffffffffffffffff").startsWith("HTTP/1.1 401")) throw new Exception("Wrong key accepted");
            if (!request(port, "/settings", key).startsWith("HTTP/1.1 404")) throw new Exception("Unexpected endpoint accepted");
            System.out.println("PASS: actual HTTP server response, access control, endpoint isolation and no caching.");
            if (args.length > 0) {
                String host = null;
                for (NetworkInterface ni : Collections.list(NetworkInterface.getNetworkInterfaces())) {
                    if (!ni.isUp() || ni.isLoopback()) continue;
                    for (InetAddress ip : Collections.list(ni.getInetAddresses())) if (ip instanceof Inet4Address && ip.isSiteLocalAddress()) { host = ip.getHostAddress(); break; }
                    if (host != null) break;
                }
                if (host == null) throw new Exception("No private IPv4 interface available for Windows integration test");
                Process client = new ProcessBuilder(args[0], "--network", host + ":" + port).inheritIO().start();
                if (!client.waitFor(20, TimeUnit.SECONDS)) { client.destroyForcibly(); throw new Exception("Windows integration test timed out"); }
                if (client.exitValue() != 0) throw new Exception("Windows integration test failed");
            }
        }
    }
}
