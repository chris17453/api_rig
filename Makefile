.PHONY: clean

clean:
	find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name "TestResults" -exec rm -rf {} + 2>/dev/null || true
	find . -type f -name "*.user" -delete 2>/dev/null || true
	find . -type f -name "*.db" -delete 2>/dev/null || true
	find . -type f -name "*.db-shm" -delete 2>/dev/null || true
	find . -type f -name "*.db-wal" -delete 2>/dev/null || true
