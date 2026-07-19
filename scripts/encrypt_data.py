import argparse
import base64
import json
import os
from pathlib import Path

from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC


ITERATIONS = 250_000


def b64url(data):
    return base64.urlsafe_b64encode(data).decode("ascii").rstrip("=")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", default="data/etfs.raw.json")
    parser.add_argument("--output", default="data/etfs.enc.json")
    args = parser.parse_args()

    password = os.environ.get("DASHBOARD_PASSWORD")
    if not password:
        raise SystemExit("DASHBOARD_PASSWORD environment variable is required.")

    plaintext = Path(args.input).read_bytes()
    salt = os.urandom(16)
    iv = os.urandom(12)
    key = PBKDF2HMAC(
        algorithm=hashes.SHA256(),
        length=32,
        salt=salt,
        iterations=ITERATIONS,
    ).derive(password.encode("utf-8"))

    payload = {
        "version": 1,
        "algorithm": "AES-GCM",
        "kdf": "PBKDF2-SHA256",
        "iterations": ITERATIONS,
        "salt": b64url(salt),
        "iv": b64url(iv),
        "ciphertext": b64url(AESGCM(key).encrypt(iv, plaintext, None)),
    }
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)
    Path(args.output).write_text(json.dumps(payload, separators=(",", ":")), encoding="utf-8")
    print(f"Wrote {args.output}")


if __name__ == "__main__":
    main()
