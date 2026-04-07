# Security Notes

## Design Philosophy

This application is designed for **transparent, application-level isolation**. The OS can see everything, but other **applications on the same machine cannot intercept or monitor** the screen-sharing activity.

## Encryption

### AES-256-CBC

- **Key Length:** 256 bits (32 bytes)
- **Mode:** CBC (Cipher Block Chaining)
- **Padding:** PKCS7
- **IV:** Random per frame, sent with each packet

**Flow:**
```
Plaintext (JPEG) → AES Encrypt → Ciphertext (Base64)
                                ↓
                              Base64 IV
                                ↓
                            Frame Packet → WebSocket → Server
```

**Decryption (server-side):**
```
Frame Packet → Extract IV + Ciphertext → AES Decrypt → Plaintext (JPEG)
```

### Key Management

**Current Implementation:**
- Key generated once during setup
- Stored in `CaptureConfig` (in memory)
- Can be manually changed via settings

**Production Recommendations:**
- Store key in Windows Credential Manager
- Use Key Derivation Function (PBKDF2) for password-based key
- Implement key rotation policy
- Never log keys

## Network Security

### WebSocket (WSS)

- **Protocol:** WebSocket Secure (TLS 1.3+)
- **Port:** 443 (standard HTTPS)
- **Certificate Validation:** Enabled by default

**To enforce certificate pinning:**

```csharp
// In NetworkService.cs, before ConnectAsync():
var certificateFile = "path/to/cert.pem";
var cert = new X509Certificate2(certificateFile);

_webSocket = new ClientWebSocket();
_webSocket.Options.ClientCertificates.Add(cert);
// Or use pinning library: https://github.com/dotnet/runtime/issues/45680
```

### Frame Packet Structure

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-03-19T05:53:00Z",
  "data": "...(base64 encrypted JPEG)...",
  "iv": "...(base64 IV)...",
  "width": 1920,
  "height": 1080,
  "version": "1.0"
}
```

**Size estimate:** ~1-2 MB per frame at 75% quality
**Bandwidth:** At 1fps, ~1-2 MB/s

## Process Isolation

### Windows Security Features

1. **User Context**
   - Runs as current user
   - No elevation required (unless running as service)
   - Respects NTFS file permissions

2. **Process Memory**
   - Other applications cannot read process memory
   - Use `ProcessHacker` or `WinDbg` to verify (admin access required)

3. **Network Isolation**
   - Only connects to configured endpoint
   - No incoming connections accepted
   - Can be further restricted via Windows Firewall

### Firewall Rules

To restrict outbound connections to only the configured endpoint:

```powershell
# As Administrator
netsh advfirewall firewall add rule name="VedionScreenShare" `
  dir=out action=allow program="C:\Program Files\Vedion\VedionScreenShare.exe" `
  remoteip=<ENDPOINT_IP> remoteport=443 protocol=tcp
```

## What This Does NOT Protect Against

⚠️ **Important:** This app is NOT designed to protect against:

1. **System-Level Attacks**
   - Kernel rootkits
   - BIOS/firmware modifications
   - DMA attacks
   - Hypervisor-level spying

2. **Physical Access**
   - Screen capture devices
   - Keyboard logging hardware
   - Network packet capture (if on shared network)

3. **User Behavior**
   - Sharing the encryption key with others
   - Running on compromised system
   - Weak passwords/credentials

## What This DOES Protect Against

✓ **Application-level isolation:**

1. **Other Applications** cannot read:
   - The encryption key
   - Unencrypted JPEG frames
   - Network packet contents
   - The WebSocket connection details

2. **Network Eavesdropping** (WSS encryption):
   - Frames are encrypted in transit
   - IV is random per frame
   - Replaying old packets doesn't reveal new content

3. **Data Exfiltration**:
   - Sensitive information shared with you is encrypted
   - Other apps cannot intercept or log it
   - Only the configured endpoint receives frames

## Threat Model

### Attacker: Spyware/Malware (User-Level)

**Goal:** Log or intercept screen-sharing activity

**Our Defense:**
- Frames encrypted with AES-256
- Malware cannot read unencrypted data
- Malware cannot read encryption key (separate process memory)
- Malware can see network traffic but not contents (TLS encryption)

**Remaining Risk:**
- Malware could monitor WebSocket packets (encrypted, harmless)
- Malware could detect that "something is being sent" (metadata)

### Attacker: Network Eavesdropper

**Goal:** Intercept and decrypt frames

**Our Defense:**
- TLS 1.3 encryption in transit
- AES-256 encryption at rest
- Random IVs prevent pattern analysis

**Remaining Risk:**
- If endpoint is compromised, attacker sees plaintext frames
- If encryption key is leaked, historical frames can be decrypted

### Attacker: Your Own IT Department (Organization)

**Goal:** Monitor what you're sharing

**Our Defense:**
- Encrypted frames not readable without key
- Frames sent over TLS (encrypted in transit)
- Application isolation makes detection harder

**Remaining Risk:**
- Kernel-level monitoring can still see screen content (before capture)
- Packet inspection can see "something" is being sent

## Recommendations for Hardening

1. **Key Rotation**
   - Generate new key monthly
   - Change endpoint URL if possible
   - Keep old keys in secure storage (if need to decrypt old frames)

2. **Endpoint Security**
   - Use strong TLS certificate (not self-signed)
   - Enable certificate pinning in app
   - Implement server-side authentication
   - Log all connections for audit

3. **Network Security**
   - Use VPN if on shared network
   - Restrict to home/trusted networks
   - Monitor for DNS hijacking

4. **Application Security**
   - Keep .NET runtime updated
   - Monitor for CVEs in dependencies
   - Run antivirus regularly
   - Disable unnecessary Windows services

5. **Operational Security**
   - Only share sensitive information during sessions
   - Delete old frames from server regularly
   - Assume frames can be recovered from disk
   - Use full-disk encryption (BitLocker)

## Compliance & Privacy

### GDPR / Privacy Regulations

⚠️ **Note:** Transmitting screen recordings may contain personal data. Ensure:

- Server endpoint is in compliant region
- User consent is obtained (setup wizard does this)
- Data is encrypted in transit and at rest
- Frames are deleted after retention period
- User can request deletion of all frames

### HIPAA / Healthcare Data

If screen contains health information:
- Use HIPAA-compliant endpoint
- Implement additional encryption (Backup Key)
- Log all access to frames
- Conduct risk assessments

## Testing Security

### Unit Tests

```csharp
[TestMethod]
public void Encrypt_WithValidKey_ReturnsEncryptedData()
{
    var service = new EncryptionService(EncryptionService.GenerateKey());
    var plaintext = Encoding.UTF8.GetBytes("Test data");
    
    var (ciphertext, iv) = service.Encrypt(plaintext);
    
    Assert.IsNotNull(ciphertext);
    Assert.IsNotNull(iv);
    Assert.AreNotEqual(Convert.ToBase64String(plaintext), ciphertext);
}

[TestMethod]
public void Decrypt_WithCorrectIv_ReturnsPlaintext()
{
    var key = EncryptionService.GenerateKey();
    var service = new EncryptionService(key);
    var plaintext = Encoding.UTF8.GetBytes("Test data");
    
    var (ciphertext, iv) = service.Encrypt(plaintext);
    var decrypted = service.Decrypt(ciphertext, iv);
    
    CollectionAssert.AreEqual(plaintext, decrypted);
}
```

### Integration Tests

- Test WebSocket connection with valid/invalid certificates
- Test encryption key validation
- Test frame serialization/deserialization
- Test network timeouts and reconnection

### Security Audit Checklist

- [ ] No hardcoded credentials or keys
- [ ] No logging of sensitive data (keys, raw frames)
- [ ] No buffer overflows or integer overflows
- [ ] Encryption is used correctly (IV random, key length correct)
- [ ] Process memory is not dumped to disk
- [ ] Error messages don't leak information
- [ ] No SQL injection or code injection vectors
- [ ] TLS certificate validation is enabled

## References

- [OWASP Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)
- [.NET Cryptography Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/security/cryptography-model)
- [WebSocket Security (RFC 6455)](https://tools.ietf.org/html/rfc6455#section-10)
- [Windows Security Best Practices](https://learn.microsoft.com/en-us/windows/security/)
